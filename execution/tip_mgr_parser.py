import yaml
import xml.etree.ElementTree as ET
import argparse
import os

class TipManagerParser:
    @staticmethod
    def parse_tip_mgr(template_fname, input_fname, base_name):
        # load data
        tree = ET.parse(template_fname)
        root = tree.getroot()
        tipnames = [x.find('ToolSizeTag').text for x in root[0][0].findall('TipItem')]

        with open(input_fname, 'r') as stream:
            input = yaml.safe_load(stream)

        # process each tip rack in the input
        for rack in input['tip racks']:
            if (rack['tip'] not in tipnames):
                raise ValueError('{} in rack {} is not a valid tip type'.format(rack['tip'], rack['rack']))

            for tiprackitem in root[0][0].findall('TipRackItem'):
                if (tiprackitem.find('Name').text == 'Off Deck 2-3 Tip Rack {}'.format(rack['rack'])):
                    for tip_type in tiprackitem.findall('PlateItem'):
                        if (tip_type.find('ToolSizeTag').text == rack['tip']):
                            tip_type.find('IsCurrent').text = 'True'
                            cols = int(tip_type.find('PlateColumns').text)
                            rows = int(tip_type.find('PlateRows').text)
                            tip_type.find('RemainingTips').text = str(rack['remaining'])
                            tip_type.find('LastRow').text = str(rows - (rack['remaining'] // cols))
                            tip_type.find('LastColumn').text = str(cols - (rack['remaining'] % cols))

        ET.indent(tree, space='\t', level=0)
        tree.write('{}_tips.xml'.format(base_name))

parser = argparse.ArgumentParser()
parser.add_argument('fname', type=str, help='input tip manager yaml file')
parser.add_argument('basename', type=str, help='experiment design file base name. E.g. the base name of "mydesign_999.lsr" and "mydesign_999_cm.xml" is "mydesign_999"')
args = parser.parse_args()

src_dir = os.path.dirname(os.path.abspath(__file__))
template_fname = os.path.join(src_dir, 'tip_mgr_template.xml')

TipManagerParser.parse_tip_mgr(template_fname, args.fname, args.basename)