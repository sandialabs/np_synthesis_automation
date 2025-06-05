from model_interface import ModelInterface
import argparse
import os

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    mode_group = parser.add_mutually_exclusive_group(required=True)
    mode_group.add_argument('--design', action='store_true', help='Set to generate experiment design files')
    mode_group.add_argument('--generate', action='store_true', help='Set to save design to database and generate chemical manager and prompts files')
    mode_group.add_argument('--run', action='store_true', help='Set to run saved design in Automation Studio')
    parser.add_argument('--exp_dir', type=str, help='Experiment directory')
    parser.add_argument('--campaign_dir', type=str, help='Campaign directory')
    parser.add_argument('--shuffle', action='store_true', help='Shuffle sample locations in experiment design')
    parser.add_argument('--design_id', type=int, nargs='*', default=None, help='Design ID to run with --run command. By default, runs the design with the highest design id in the experiment director')
    parser.add_argument('--design_input', type=str, default='design_input.yaml', help='(Optional) Specified design input to save to database. Default = design_input.yaml')
    args = parser.parse_args()

    if (args.design):
        model_interface = ModelInterface(args.exp_dir, args.campaign_dir)
        df = model_interface.load_input(args.shuffle)
        df = model_interface.generate_experiment(df)
        model_interface.save_model_output(df)
    elif (args.generate):
        model_interface = ModelInterface(args.exp_dir, args.campaign_dir)
        model_interface.save_to_database(args.design_input)
    elif (args.run):
        model_interface = ModelInterface(args.exp_dir, args.campaign_dir)
        model_interface.start_AS()
        if (args.design_id is not None):
            for id in args.design_id:
                model_interface.run_design(id)
                model_interface.monitor_design()