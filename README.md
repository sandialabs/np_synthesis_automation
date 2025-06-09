# Insert title here
TODO: description

## Dependencies
This code depends on the Library Studio and Automation Studio APIs by Unchained Labs. Contact them for access and instructions for installing.

`designmanager` depends on the Library Studio API. Copy its `Libs` and `SDK` directories into this repository's root directory

`asmanager` depends on the Automation Studio API/SiLA Client. From the sample SiLA client project provided by Unchained Labs, copy these directories/files into the `asmanager` directory:
```
AutomationStudio/
AutomationStudioRemote/
Dependencies/
ExperimentService/
ExperimentStatusService/
Properties/
RunService/
ViewModels/
ConsoleLogging.cs
ServerDiscovery.cs
ServerInfo.cs
```

## Installation
1. Clone this repo
```
git clone TODO: url
```

2. Use the provided conda environment definition `environment.yml` to build the conda environment:
```
conda env create -f environment.yml
```

3. Open `designmanager/DesignManager.sln` and build the solution.

4. Open `asmanager/SiLAClient.sln` and build the solution.

## Usage
The following instructions describe how to generate an experiment definition from a template, save it to the database, and run it on the Big Kahuna. For more details, refer to the [file details](#file-details)

1. Ensure the experiment directory has the necessary files
    1. Define experiment in `design_template.yaml`
    2. Define changing variables in `input.csv`
    3. To create multiple designs from a single template, insert a type: split_design map in the maps to indicate where the previous design should end and a new one should begin
    4. Automation studio may require a well plate to have a vial with a non-zero volume before it can be drawn from. To work around this, define a dispense step into the plate and add Dummy to the list of that dispense step’s tags
2. Generate input deck
    1. Run `python model_execution.py --design --exp_dir <PATH TO EXPERIMENT DIRECTORY> --campaign_dir <PATH TO CAMPAIGN DIRECTORY>`
    2. An optional `--shuffle` flag can be provided to shuffle the input rows
    3. Ensure an `exp_input.csv` file is created, which tracks how input rows line up with stunner output rows
    4. If the design template has no splits, this will generate a `design_input.yaml` in the specified experiment directory. Otherwise, it will generate a `design_input_##.yaml` for each separate design
    5. For notes on running the model_execution script, you can run `python model_execution.py --help`
3.	Parse input deck to generate design in database
    1. Run `python model_execution.py --generate --exp_dir <PATH TO EXPERIMENT DIRECTORY> --campaign_dir <PATH TO CAMPAIGN DIRECTORY>`
    2. By default, this expects a file in the experiment directory called `design_input.yaml`
    3. Use the `--design_input` flag to specify the design input file to use (eg. `design_input_00.yaml` if the template were split) instead of the default
    4. This will generate an `<EXPERIMENT NAME>_<DATABASE ID>.lsr`, `<EXPERIMENT NAME>_<DATABASE ID>_cm.xml`, and `<EXPERIMENT NAME>_<DATABASE ID>_prompts.xml` file in the experiment directory
4.	Execute BK experiment
    1. Make sure Automation Studio is closed.
    2. Run `python model_execution.py --run --exp_dir <PATH TO EXPERIMENT DIRECTORY> --campaign_dir <PATH TO CAMPAIGN DIRECTORY> --design_id <id 1, …>`
    3. One or more design ids can be specified to run in sequence, but this expects a `.lsr`, `_cm.xml`, and `_prompts.xml` file for each specified id.
    4. Once the run is started, the status will be monitored and printed to console. This process can be killed with ctrl+c. This will not abort the run. To abort, manually do so in Automation Studio.

## File details
`design_template.yaml`: Defines the template for the experiment design. See `designmanager/test_input.yaml` for a sample of possible maps to execute. Templated maps are marked by `s1, s2, ... s#` rather than numerical values, indicating that the corresponding row and column from the `input.csv` will be used to populate the field.

`input.csv`: Defines values for the parameters to populate the design template to create a completed experiment definition. This consists of columns that define dispense volums for relevant chemicals or stunner analysis parameters. Each row represents a single unique sample.

TODO: sample problem