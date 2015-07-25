#Leap Motion Data Capture

**Purpose**

Simple high performance timestamped Leap Motion data recording program

**Capabilities**

Record ALL leap motion data to binary file format. Format is as follows:

1)	64 bit integer: Serialised C# DateTime object, can deserialise with FromBinary

2)	32 bit integer: Current Stimcode

3)	32 bit integer: Length of following data

4)	Integer of length defined by (3): Serialised leap motion frame data, can deserialise with SDK

Code already exists in the project in MainWindows.xaml.cs in btnChooseFile_Click to read the data into an array of frames. Since the number of fields is too high to put into a CSV reasonably, one has to take the relevant data from the read frame array that the program has.  In Button_Click, basic code has already been laid out for looping through each frame, modify at will.


**Usage**

1)	Open program, use Record Start to start recording. 

2) The prefix for file names can be changed before the click. The .data file is the raw file, the .csv file does not exist, just dummy. 

3) Press Record Stop/Pause/Resume to do the respective actions. 

4) The playback tab has a button that reads all frames from that file into an array. Extensibility is left to the user of this code, as there are too many data points in a frame to reasonably save to a .csv file (or maybe i'm just lazy to iterate through them all). Perhaps on a future date.