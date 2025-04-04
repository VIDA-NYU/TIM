# Hololens Desktop ML

This a Python setup that pulls sensor data from the Hololens using sockets, performs some machine learning,
and sends the machine learning results back to the Hololens to display on the screen. This also contains
tools for testing the functionality without a Hololens present (i.e. it can stream video from files or 
your webcam to emulate a hololens).

## Installation

```python
# clone the repo
git clone git@github.com:VIDA-NYU/ptg-alphatesting.git
cd ptg-alphatesting
git checkout remote/origin/streaming
cd Streaming/python

# setup a python environment
conda create -n ptg python=3.9
conda activate ptg
conda install numpy  # you may need to do this first ??
pip install -r requirements.txt

```

## Connecting to a real-life Hololens

1. Get the IP address of your hololens. As an example, we are using `192.168.4.56`
2. Start the app on the hololens
3. Then run:

```bash
python main.py desktop --holo-ip 192.168.4.56
```

This will start a socket that will listen for camera frames and will do ML and display to screen.

To additionally start a server to send them back to the hololens display, do:

```bash
python main.py desktop --holo-ip 192.168.4.56 --send
```

`--no-draw` is optional, this will just disable showing the video and boxes on the desktop. Eventually, the default will be switched 

## But I don't have a Hololens!! - It's cool! You can just run things locally

First run the desktop app (no need for an IP address cuz it's local):

```bash
# default settings draw video and boxes
python main.py desktop
# pick a different model
python main.py desktop --model yolov3
# start a server that sends detections back to the hololens
python main.py desktop --model yolov3 --send
```

Then run the simulated hololens:

```bash
# use your webcam
python mock.py holo --src 0
# use a video
python mock.py holo --src path/to/video.mp4 --fps 16
```

To simulate the display client do:

```bash
# will plot the bounding boxes on a black image (no video comm needed between the two hololens apps)
python mock.py draw
```


## Selecting a model

By default it's set at tinyyolov2 seeing as that's what the original project uses.

You can show all available models. Names are case insensitive.

```bash
python main.py models show
```

Then just pick one!

```bash
python main.py desktop --model yolov3
```

## Profiling

You can profile each block (each of which represents either a separate thread or a separate process).

This can show you where bottlenecks are in the system. If a block spends most of its time sleeping, then it's not 
a bottleneck. Look for blocks that spend a lot of time doing something else.

```bash
# in case it's not already installed
pip install pyinstrument
```

```bash
python mock.py holo --src 0 --profile
python main.py desktop --profile
python mock.py draw --profile
```

# Model Demos
There's some great demos out there! But they're bundled into notebooks so they're hard to repurpose.

Here I'm just pulling out the code, cleaning it up and packaging them in simple classes for re-use and experimentation.

## Omnivore

Original Demo: https://colab.research.google.com/github/facebookresearch/omnivore/blob/main/inference_tutorial.ipynb#scrollTo=vaYFMsE6-bSU

Run:

```bash
# run the default - image, video, image+depth
python omnivore/omnivore.py

# run your own image or video: {input_src} {input_type} where input_type = {image,video,depth}
python omnivore/omnivore.py omnivore/OJ.mp4 video --offset 14
```

## MDETR

Demo: https://colab.research.google.com/github/ashkamath/mdetr/blob/colab/notebooks/MDETR_demo.ipynb#scrollTo=3TEPUFkaKCZt

Code: `mdetr/`

Run the three different models:

|                          |  model size |  cpu time   |
|--------------------------|-------------|-------------|
| MDETR                    | 2.53G model | 4s          |
| MDETR Segmentation       | 1.16G model | 10s         |
| MDETR Question-Answer    | 2.54G model | 4s          |

```bash
# regular - umbrellas
python mdetr/mdetr.py
# segmentation - hotel
python mdetr/mdetr.py --model mask
# question-answer - bus stop
python mdetr/mdetr.py --model qa
```

## VILD

Demo: https://colab.research.google.com/github/tensorflow/tpu/blob/master/models/official/detection/projects/vild/ViLD_demo.ipynb

Code: `vild/`

```bash
python vild/vild.py
```

## GPv2
This one is already packaged up so just use from their repo


Code: https://github.com/allenai/gpv2/

```bash
# download code
git clone git@github.com:allenai/gpv2.git --recurse-submodules
cd gpv2

# install dependencies
conda create -n gpv2 python=3.6 -y
conda activate gpv2
# conda install pytorch==1.8.1 torchvision==0.9.1 torchaudio==0.8.1 cudatoolkit=11.2 -c pytorch -c conda-forge
conda install pytorch==1.8.1 torchvision==0.9.1 torchaudio==0.8.1 -c pytorch -c conda-forge  # or instead for cpu
conda install -c cyclus java-jdk=8.45.14 -y  
pip3 install -r requirements.txt

# download models
mkdir -p models
aws s3 cp --recursive s3://ai2-prior-gpv/public/gpv2-models/gpv2 models/gpv2

# eval on an image
python gpv2/eval/run_on_image_id.py models/gpv2 path/to/image.jpg "What is this?"
```

I'm having trouble with s3 and downloading the models
 - I was hoping to try downloading directly from the browser, but access denied https://ai2-prior-gpv.s3.amazonaws.com/gpv2-models/gpv2

need to figure that out
