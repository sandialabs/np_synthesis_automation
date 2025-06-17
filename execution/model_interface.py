import numpy as np
import yaml
import pandas as pd
import os
import re
from datetime import datetime
import subprocess
import clr
import json
import glob
import time
import copy
from string import Template
from stunner_api_template import *

script_dir = os.path.dirname(os.path.abspath(__file__))
with open(os.path.join(script_dir, 'dependencies.yaml'), 'r') as stream:
    dependencies = yaml.safe_load(stream)

clr.AddReference(dependencies['silaclient'])
from SiLAClient import ASManager

class ModelInterface:
    def __init__(self, exp_dir, campaign_dir):
        self.exp_dir = exp_dir
        self.campaign_dir = campaign_dir

        self.campaign_results_dir = os.path.join(self.campaign_dir, 'results')
        if (not os.path.isdir(self.campaign_results_dir)):
            os.mkdir(self.campaign_results_dir)

        self.design_manager_exe = dependencies['designmanager']

        self.template_pattern = re.compile("s[0-9]+")

    def load_input(self, shuffle):
        df = pd.read_csv(os.path.join(self.exp_dir, 'input.csv'))
        if (shuffle):
            self.shuffle_inds = np.arange(0, len(df))
            np.random.shuffle(self.shuffle_inds)
            df = df.reindex(self.shuffle_inds)

        sample_col = ['s{}'.format(x) for x in range(1, len(df) + 1)]
        df['sample'] = sample_col

        return df

    def save_model_output(self, df):
        # drop blanks
        df = df[df['stunner_sample_type'] == 'S']

        df.to_csv(os.path.join(self.exp_dir, 'exp_input.csv'), index=False)

    def generate_experiment(self, df):
        # read in template yaml file and find steps that require filling in
        with open(os.path.join(self.exp_dir, 'design_template.yaml'), 'r') as stream:
            template = yaml.safe_load(stream)

        pos_map, stunner_map = self.samplenum_to_position(template)
        dt_now = datetime.now()
        df['sample_name'] = [ModelInterface.sample_to_id(x, dt_now) for x in df['sample']]
        df['sample_library'] = [pos_map[x][0] for x in df['sample']]
        df['sample_position'] = [pos_map[x][1] for x in df['sample']]
        df['stunner_position'] = [stunner_map[x] for x in df['sample']]

        # first determine how many designs to split into
        num_designs = 1
        for map in template['maps']:
            if (map['type'] == 'split_design'):
                num_designs += 1

        # also keep track of which chemicals and libraries are used in each design
        # so that we can go back and delete the ones that aren't used in each design
        designs = []
        design_libs = []
        design_chems = []
        for _ in range(0, num_designs):
            design = copy.deepcopy(template)
            design['maps'] = []
            designs.append(design)
            design_libs.append(set())
            design_chems.append(set())

        design_count = 0
        run_stunner = False
        for map in template['maps']:
            if (map['type'] == 'split_design'):
                design_count += 1
                continue

            elif (map['type'] == 'dispense'):
                chemical = map['chemical']
                for i in range(0, len(map['map'])):
                    mapval = map['map'][i]
                    if (isinstance(mapval, str) and self.template_pattern.match(mapval)):
                        val = df[chemical][df['sample'] == mapval].iloc[0]
                        if (isinstance(val, np.floating)):
                            val = float(val)
                        map['map'][i] = val

            elif (map['type'] == 'stunner_sample_type'
                  or map['type'] == 'stunner_sample_group'
                  or map['type'] == 'stunner_analyte_name'
                  or map['type'] == 'stunner_buffer_name'
                  or map['type'] == 'stunner_application'):
                col = map['type']
                for i in range(0, len(map['map'])):
                    mapval = map['map'][i]
                    if (isinstance(mapval, str) and self.template_pattern.match(mapval)):
                        val = df[col][df['sample'] == mapval].iloc[0]
                        if (isinstance(val, np.floating)):
                            val = float(val)
                        map['map'][i] = val

            elif (map['type'] == 'start_stunner'):
                run_stunner = True

            # store any libraries or chemicals that are used
            if ('library' in map):
                design_libs[design_count].add(map['library'])
            if ('source' in map):
                design_libs[design_count].add(map['source'])
            if ('chemical' in map):
                design_chems[design_count].add(map['chemical'])

            designs[design_count]['maps'].append(map)

        # keep only libraries and chemicals used in each design
        for i, design in enumerate(designs):
            design['libraries'] = [x for x in design['libraries'] if x['name'] in design_libs[i]]
            design['chemicals'] = [x for x in design['chemicals'] if x['name'] in design_chems[i]]

        # write out all design files
        if (num_designs == 1):
            with open(os.path.join(self.exp_dir, 'design_input.yaml'), 'w') as f:
                yaml.dump(designs[0], f, default_flow_style=None)
        else:
            for i, design in enumerate(designs):
                with open(os.path.join(self.exp_dir, 'design_input_{:02d}.yaml'.format(i)), 'w') as f:
                    yaml.dump(designs[i], f, default_flow_style=None)

        # separately write out input strings needed for stunner API
        if (run_stunner):
            stunner_samples, stunner_cols, stunner_results = self.design_to_stunner_samples(template, df)
            with open(os.path.join(self.exp_dir, 'stunner_samples.txt'), 'w') as f:
                f.write(stunner_samples)

            with open(os.path.join(self.exp_dir, 'stunner_cols.txt'), 'w') as f:
                f.write(stunner_cols)

            with open(os.path.join(self.exp_dir, 'stunner_results.txt'), 'w') as f:
                f.write(stunner_results)

        return df

    def samplenum_to_position(self, design):
        # use design template to associate sample numbers (s#) to plate positions
        # and then associate those to stunner positions
        pos_map = {}
        stunner_map = {}
        for map in design['maps']:
            if (map['type'] == 'dispense'):
                # get the library dimensions
                libname = map['library']
                for lib in design['libraries']:
                    if (lib['name'] == libname):
                        rows = lib['rows']
                        cols = lib['cols']

                # get mapping of s# to plate positions
                for i in range(0, len(map['map'])):
                    mapval = map['map'][i]
                    if (isinstance(mapval, str) and self.template_pattern.match(mapval)):
                        r = int(i/cols)
                        c = i % cols
                        pos = (libname, chr(ord('A') + r) + str(c + 1))
                        if mapval in pos_map:
                            if (pos_map[mapval] != pos):
                                raise ValueError('Sample position {} is inconsistent! Previously was set to {} and now is set to {}'.format(mapval, pos_map[mapval], pos))
                        else:
                            pos_map[mapval] = pos

        # hard code well plate to stunner plate mapping
        # assume A1-H6 are occupied on stunner plate
        stunner_rows = 8
        stunner_cols = 6
        for key in pos_map:
            ind = int(key.strip('s')) - 1
            pos = self._ind_to_plate_pos(ind, stunner_rows, stunner_cols)
            stunner_map[key] = pos

        return pos_map, stunner_map

    def save_to_database(self, fname):
        input_fname = os.path.join(self.exp_dir, fname)
        if (not os.path.isfile(input_fname)):
            raise FileNotFoundError("conversion of {} requested but does not exist in provided directory {}".format(fname, self.exp_dir))

        cur_dir = os.getcwd()
        os.chdir(self.exp_dir)
        result = subprocess.run([self.design_manager_exe, input_fname], capture_output=True, text=True)
        print(result.stdout)
        print(result.stderr)
        os.chdir(cur_dir)

    @staticmethod
    def sample_to_id(num, dt_now):
        now_str = dt_now.strftime("%Y-%m-%d-%H-%M-%S")
        return '{}-{}'.format(now_str, num)

    def start_AS(self):
        self.asmanager = ASManager()
        status = json.loads(self.asmanager.GetStatus())
        if (status['Content'] == 'Experiment running'):
            raise ValueError('Cannot run design. An experiment is already running')

    def run_design(self, design_id):
        # find required design files
        if (design_id is None):
            files = glob.glob(os.path.join(self.exp_dir, '*.lsr'))
            base_fnames = [os.path.splitext(os.path.basename(x))[0] for x in files]
            design_ids = [int(x.split('_')[-1]) for x in base_fnames]
            design_id = sorted(design_ids)[-1]

        prompts_file = self._find_file_by_id(self.exp_dir, '*prompts.xml', design_id)
        cm_file = self._find_file_by_id(self.exp_dir, '*cm.xml', design_id)
        tips_file = self._find_file_by_id(self.exp_dir, '*tips.xml', design_id, True)

        if (tips_file is None):
            print('WARNING: tip manager file not found. Using current tip manager state in AS')
            msg = self.asmanager.RunDesign(design_id, prompts_file, cm_file)
        else:
            msg = self.asmanager.RunDesign(design_id, prompts_file, cm_file, tips_file)

    def monitor_design(self):
        is_exp_done = False
        while not is_exp_done:
            exp_status = json.loads(self.asmanager.GetExperimentStatus())
            prompt = json.loads(r'{}'.format(self.asmanager.PollPromptRaw()))
            is_exp_done = exp_status['Content']['Status'] == 'Experiment completed'

            status_str = 'Current time: {}\nCurrent experiment status: {}\nCurrent action: {}\nCurrent map: {}\n'.format(
                datetime.now().strftime("%Y-%m-%d_%H:%M:%S"), exp_status['Content']['Status'],
                exp_status['Content']['CurrentAction'], exp_status['Content']['CurrentMap'])
            if (prompt['StatusCode'] == 0):
                content = json.loads(prompt['Content'])
                prompt_msg = content['InformationMessage']
                prompt_title = content['Title']
                prompt_options = content['Option'].split('|')
                status_str += 'Current prompt: {}: {}\nOptions: {}\n'.format(prompt_msg, prompt_title, prompt_options)

                if ('Paused for stunner measurement' in prompt_msg):
                    self.asmanager.SetPromptInput('OK')
            print(status_str, flush=True)

            time.sleep(5)

    def design_to_stunner_samples(self, design, df):
        sample_def = {
            'experiment_name': design['design']['name']
        }
        stunner_rows = 8
        stunner_cols = 12
        stunner_samples = {}
        for i in range(0, 96): # assume 96 wellplate for stunner
            stunner_samples[self._ind_to_plate_pos(i, stunner_rows, stunner_cols)] = {}

        # process the design and convert to the column format needed for the stunner api
        for map in design['maps']:
            if (map['type'] == 'stunner_application'):
                applications = [x for x in map['map'] if x is not None]
                # assume all samples use the same application name
                if (len(set(applications)) > 1):
                    raise ValueError("Multiple application names in stunner_application detected. All samples must use the same application name")
                sample_def['application_name'] = applications[0]
            elif (map['type'] == 'stunner_sample_type'
                  or map['type'] == 'stunner_sample_group'
                  or map['type'] == 'stunner_analyte_name'
                  or map['type'] == 'stunner_buffer_name'):
                for i in range(0, len(map['map'])):
                    mapval = map['map'][i]
                    if (mapval is None):
                        continue
                    stunner_pos = self._ind_to_plate_pos(i, stunner_rows, stunner_cols)
                    if (map['type'] == 'stunner_sample_type'):
                        if (mapval == 'B'):
                            stunner_samples[stunner_pos]['sample_name'] = 'blank1'
                        else:
                            stunner_samples[stunner_pos]['sample_name'] = df.loc[df['stunner_position'] == stunner_pos, 'sample_name'].reset_index(drop=True)[0]
                    elif (map['type'] == 'stunner_sample_group'):
                        stunner_samples[stunner_pos]['sample_group'] = mapval
                    elif (map['type'] == 'stunner_analyte_name'):
                        stunner_samples[stunner_pos]['analyte'] = mapval
                    elif (map['type'] == 'stunner_buffer_name'):
                        stunner_samples[stunner_pos]['buffer'] = mapval

        # write all the samples to a single string
        samples_outstr = ''
        for stunner_pos in stunner_samples:
            if (len(stunner_samples[stunner_pos]) == 0):
                continue

            samples_outstr += 'Plate 1,{},{},{},{},{}\n'.format(
                stunner_pos,
                stunner_samples[stunner_pos]['sample_name'],
                stunner_samples[stunner_pos]['sample_group'],
                stunner_samples[stunner_pos]['analyte'],
                stunner_samples[stunner_pos]['buffer'],
            )

        columns_outstr = COLUMNS_TEMPLATE
        columns_outstr = Template(columns_outstr).substitute(sample_def)
        results_outstr = RESULTS_TEMPLATE

        return samples_outstr, columns_outstr, results_outstr

##############################################
# private methods
##############################################

    def _find_file_by_id(self, dir, pattern, id, allow_none=False):
        files = glob.glob(os.path.join(dir, pattern))
        for file in files:
            if (int(file.split('_')[-2]) == id):
                return os.path.abspath(file)
        if (allow_none):
            return None
        else:
            raise FileNotFoundError('Required file for design id {} not found'.format(id))

    def _check_required_files(self):
        # make sure all the experiment files exist
        experiment_files = [
            'design_template.yaml',
            'input.csv',
        ]
        file_exists = [os.path.isfile(os.path.join(self.exp_dir, x)) for x in experiment_files]
        if (not all(file_exists)):
            missing_files = []
            for i in range(0, len(file_exists)):
                if (not file_exists[i]):
                    missing_files.append(experiment_files[i])
            missing_file_str = ','.join(missing_files)
            raise FileNotFoundError("The experiment directory does not contain all required experiment files! {} are missing".format(missing_file_str))

    def _ind_to_plate_pos(self, ind, rows, cols):
        row = ind // cols
        col = ind % cols
        plate_pos = chr(ord('A') + row) + str(col + 1)
        return plate_pos