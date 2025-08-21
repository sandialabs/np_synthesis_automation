# Nanoparticle Synthesis Automation
This repository contains the code associated with the experiments described in the publication: _An Ecosystem for Realizing Underreported Silver Nanoparticle Synthesis Variables_. These include libraries for interacting with Unchained Labs' Big Kahuna and Stunner lab automation machines as well as scripts to template, define, and run synthesis and characterization experiments on these machines.

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
git clone https://github.com/sandialabs/np_synthesis_automation.git
```

2. Use the provided conda environment definition `environment.yml` to build the conda environment:
```
conda env create -f environment.yml
```

3. Open `designmanager/DesignManager.sln` and build the solution.

4. Open `asmanager/SiLAClient.sln` and build the solution.

5. Create a `dependencies.yaml` file in the `execution` directory. Populate it with the paths to the built executables:
```
designmanager: <PATH TO DesignManager.exe (step 3)>
silaclient: <PATH TO SiLAClient.dll (step 4)>
```

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

An example of both files can be found in `examples/split_design`. This example can be used to generate and run an experiment using the [usage](#usage) instructions.

## Data
Data used in the publication is in `data`. For more information see our paper.

Reaction inputs and measured resulting properties for 1700+ silver nanoparticle syntheses. Measured properties include Dynamic Light Scattering (DLS) characterized particle sizes and distributions, and UV-Visible Absorption Spectroscopy (UV-VIS) characterized solution absorbance spectra.

### Fields
**Subdirectory:** location of the original data file

**Glycolic acid concentration (mM):** the starting concentration of the precursor stock solution (not the concentration of the precursor in the reaction solution)

**Reductant:** NaBH4 = sodium borohydride; TSC = trisodium citrate; N2H4 = hydrazine monohydrate

**Order of operations permutation:** the order of precursor addition for silver nanoparticle synthesis (described in Table 1 of the main manuscript)

**Glycolic acid solution volume (mL):** the volume of precursor used in silver nanoparticle synthesis

**stunner_sample_type:** designates each entry as either “S” (sample) or “B” (blank)

**sample_name:** unique sample identifier

**sample_position:** sample location on the 4×6 substrate used to hold the silver nanoparticle samples prepared with the Big Kahuna

**stunner_position:** the corresponding location of each silver nanoparticle sample in the Stunner microfluidic well plate

**# peaks:** the total number of peaks observed in the DLS spectrum

**Intercept:** the intercept of the correlation function obtained during DLS aquisition

**Z Ave. Dia (nm):** (Z-average diameter) the intensity-weighted average particle size calculated using the cumulants method

**PdI:** (polydispersity index) indicates the heterogeneity or broadness of a particle size distribution.

**SD Dia (nm):** the standard deviation of the DLS particle size distribution

**Diffusion coefficient (um^2/s):** calculated from the scattering intensity as a function of time using the Stokes-Einstein equation

**Peak of Interest:** the “primary” peak in the DLS spectrum as determined by the molecular weight of the analyte

**Mean Dia (nm):** the average particle diameter of all values recorded

**Mode Dia (nm):** the particle diameter most frequently recorded

**Est. MW (kDa):** the estimated molecular weight of the nanoparticle species

**Intensity (%):** the percentage of the total scattered light intensity of the sample

**Mass (%):** the percentage of the total mass distribution of the sample

**Derived intensity (cps):** the intensity of scattered light associated with different populations of particles or molecules within a sample, derived from the raw DLS signal.

**Rayleigh ratio R (cm^-1):** measure of light scattering intensity (the ratio of scattered light intensity to incident light intensity)

**Temperature (°C):** the temperature recorded by the Stunner at the time of DLS or UV-Vis measurement

**Number of acquisitions used:** the number of DLS scans used to provide the final dataset

**A280 (10mm):** the blank-corrected absorbance at 280 nm with a nominal path length of 10 mm

**Reductant age (days):** the age of the reductant solution at the time of silver nanoparticle synthesis

**Temperature (degrees C):** the temperature inside of the Big Kahuna at the time of silver nanoparticle synthesis

### Supplemental Information
Outputs include two distinct data types: scalar features extracted from DLS-based characterization of nanoparticle sizes and distributions, and UV-VIS absorption spectra from 280nm – 700nm.

## Citation
TODO: put up a bibtex or something after publication
