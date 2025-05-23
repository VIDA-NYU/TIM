# TIM: A Transparent, Interpretable, and Multimodal Personal Assistant

Hololens HUD provides a novel user interface that generates assistive stimuli in the AR headset to optimize users’ task operation.

## Installation (sideloading)

The server software is distributed as a single appxbundle file.

1. Download the latest [appxbundle](https://github.com/VIDA-NYU/tim-personal-assistant/releases/tag/v1.0.0.0).
2. Go to the Device Portal (type the IP address of your HoloLens in the address bar of your preferred web browser) and upload the appxbundle to the HoloLens (System -> File explorer -> Downloads).
3. On your HoloLens, open the File Explorer and locate the appxbundle. Tap the appxbundle file to open the installer and tap Install.
You can find the server application (Hololens HUD) in the All apps list.

Note: Please, take into account that the Hololens HUD application will be using the NYU's PTG Data Store API. To deploy a local server, please follow the [Hololens HUD Deployment (for developers)](https://github.com/VIDA-NYU/tim-personal-assistant/edit/main/ReadMe.md#hololens-hud-deployment-for-developers) instructions.

### Visualizing streaming data and models
To explore the data generated by the hololens and the models running behind the system, we provides the [TIM Dashboard](https://github.com/VIDA-NYU/tim-dashboard) system that is a Hololens data exploration platform developed at New York University. It can be used to ingest new data, explore data generated using Hololens and debug models for AR assistant systems. For an easy and quick look, the system can be found at this address: https://dashboard.ptg.poly.edu/ which is deployed at the NYU server. If you want to deploy a local server, please see [Set up the PTG Data Store API](https://dashboard-rtd.ptg.poly.edu/deployment.html#set-up-the-ptg-data-store-api) for instructions.

## Hololens HUD Deployment (for developers)

Note: Search for all occurrences of <REPLACE_WITH_SERVER_ADDRESS> in the project and replace them with the actual IP address of your server.

### Setting up a server
To deploy the Hololens HUD system locally, first you need to set up the PTG Data Store API. Instructions are available [here](https://dashboard-rtd.ptg.poly.edu/deployment.html#set-up-the-ptg-data-store-api).

### Enabling Developer Mode

Enable developer mode in both hololens and Windows PC (see [here](https://docs.microsoft.com/en-us/windows/mixed-reality/develop/advanced-concepts/using-visual-studio?tabs=hl2#enabling-developer-mode)).

### Configure the Unity project

1. Drag and drop the scene PTG_hub from **Assets > Scenes** folder in the **Project** window to the **Hierarchy** window, and remove the scene **Untitled** from the Hierarchy window.
2. In the **Project** window, navigate to the **Assets > Plugins** folder, then verify the configuration of the following files (from **Platform settings > Universal Windows Platform settings**):
      - HLStreamerUnityPlugin.dll: SDK UWP; CPU ARM64
      - HLStreamerUnityPlugin.winmd: SDK UWP; CPU AnyCPU
      - The others: SDK: Any, CPU Any
      - (Don't forget to click the **Apply** button)

### Build the application in Unity

1. In the menu bar, select **File > Build** Settings....
2. In the **Build Settings** window, then verify the **Scenes In Build** list. This must have the following scenes AND must be sorted as follow:
    - Scenes/PTG_hub             (0)
    - Scenes/PTG_v2    (1)
    - Scenes/HLStreamerScene     (2)
    
    If there is no scene listed in the **Scenes In Build** list, then click the **Add Open Scenes** button to add the previously listed scenes to the **Scenes In Build** list. 

3. Navigate to the **Platform > Universal Windows Platform** view in the **Build Settings** window. Changes this parameters:
    - Target Device: HoloLens
    - Architecture: ARM64
4. Click the **Switch Platform** button.
5. Click the **Build** button.
6. In the **Build Universal Windows Platform** window, navigate to the folder where you want to store your build, or create a new folder and navigate to it (*/tim-personal-assistant/Hololens_HUD/Build*), and then click the **Select Folder** button to start the build process.

### After Built

1. Navigate to the folder *../tim-personal-assistant/Hololens_HUD/Build/Hololens HUD/*
2. Open the file **Package.appx.manifest**
3. Replace the code ```<Package … >``` for ```<Package … >``` from [HLStreamerExample](https://github.com/VIDA-NYU/tim-personal-assistant/tree/main/Streaming/HLStreamerExample).

### Build and deploy the application in Hololens 
For detailed information on how to build and deploy in Hololens check the section ‘*(Optional) Build and deploy the application*’ [here](https://docs.microsoft.com/en-us/learn/modules/learn-mrtk-tutorials/1-7-exercise-hand-interaction-with-objectmanipulator).

1. Turn on hololens. USB cable connected Hololens and PC (in case you are using USB Cable). Uninstall the previous app in HoloLens.
2. Navigate to the folder *../tim-personal-assistant/Hololens_HUD/Build/*
3. Click Hololens HUD.sln. Change the configuration to Release, ARM64 and Device. (If want debugging choose debug)
    - (Optional) The first time you deploy an app from Visual Studio to your HoloLens, you'll be prompted for a [PIN](https://docs.microsoft.com/en-us/windows/mixed-reality/develop/advanced-concepts/using-visual-studio?tabs=hl2#pairing-your-device).
    - (Important) Set up Hololens IP by clicking **Project > Properties > Debugging > Machine Name** (Hololens IP can be found in the Hololens settings or by saying out loud "What is my IP" ).
4. Build and deploy (if debugging build and directly click Device button). To debug (recommended), click on **Debug > Start without debugging**

### Running your app on your HoloLens

1. After your app finishes building, in the HoloLens **Start** menu, find the app ‘Hololens HUD’, and then select it.
2. After the app starts, then start the backend by running ```python main.py desktop --holo-ip [ip_address] --send``` (first located in that directory *../streaming/python/*). The ip_address must be changed. 
    - To find your IP address, on your HoloLens, go to **Settings > Updates & Security > For developers**. The IP address is listed towards the bottom of the window under **Ethernet**.

### FQA

To address possible issues while deploying our app check [here](https://github.com/VIDA-NYU/tim-personal-assistant/blob/main/Hololens_HUD/Readme).

## For instructions on how to use the Desktop App
See [here](https://github.com/VIDA-NYU/tim-personal-assistant/tree/main/Streaming/python).
