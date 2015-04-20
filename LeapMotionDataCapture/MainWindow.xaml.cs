using System;
using System.Collections.Generic;
using System.Windows;
using Leap;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NamedPipeWrapper;

namespace LeapMotionDataCapture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ILeapEventDelegate
    {
        //overall controller for the leap motion device
        private Controller controller = new Controller();

        //instance of a listener that is called when events happen in the leap device
        private LeapEventListener listener;

        //checks if program is closing
        private Boolean isClosing = false;

        //global writer object
        private System.IO.BinaryWriter writer;

        //writer object for CSV
        private System.IO.StreamWriter csvWriter;

        //var to check if recording is requested
        private Boolean recordMode = false;

        //list to hold all deserialised frames
        List<Leap.Frame> frameList;

        //var to hold stimcode, writer function will clear this once it's written to the closest available data frame
        public static int curStimCode;

        //pipe stuff
        MyClient pipeClient;
        private const string PIPE_NAME = "DataCapturePipe";
        private const string LEAP_CONNECTED_MESSAGE = "-2";
        private bool firstFrame = true;
        /// <summary>
        /// Init vars and subscribe the required listener to the leap controller
        /// </summary>
        /// 
        public MainWindow()
        {
            InitializeComponent();
            this.controller = new Controller();
            this.listener = new LeapEventListener(this);
            controller.AddListener(listener);

            //init pipe stuff
            pipeClient = new MyClient(PIPE_NAME);
            pipeClient.SendMessage("I Am Leap Motion");
            
            //init the frame list
            frameList = new List<Leap.Frame>();

            //init the stimcode
            curStimCode = 0;
        }


        delegate void LeapEventDelegate(string EventName);
        public void LeapEventNotification(string EventName)
        {
            if (this.CheckAccess())
            {
                switch (EventName)
                {
                    case "onInit":
                        Debug.WriteLine("Init");
                        break;
                    case "onConnect":
                        this.connectHandler();
                        break;
                    case "onFrame":
                        if (firstFrame == true)
                        {
                            firstFrame = false;
                            pipeClient.SendMessage(LEAP_CONNECTED_MESSAGE);
                        }
                        if (!this.isClosing)
                            this.newFrameHandler(this.controller.Frame());
                        break;
                }
            }
            else
            {
                Dispatcher.Invoke(new LeapEventDelegate(LeapEventNotification), new object[] { EventName });
            }
        }

        void connectHandler()
        {
            this.controller.SetPolicy(Controller.PolicyFlag.POLICY_IMAGES);
            this.controller.SetPolicy(Controller.PolicyFlag.POLICY_BACKGROUND_FRAMES);
            this.controller.EnableGesture(Gesture.GestureType.TYPE_SWIPE);
            this.controller.Config.SetFloat("Gesture.Swipe.MinLength", 100.0f);
        }

        void newFrameHandler(Leap.Frame frame)
        {
            this.lblID.Content = "ID: " + frame.Id.ToString();
            this.lblTimestamp.Content = "Timestamp: " + frame.Timestamp.ToString();
            this.lblFPS.Content = "FPS: " + frame.CurrentFramesPerSecond.ToString();
            this.lblIsValid.Content = "IsFrameValid: " + frame.IsValid.ToString();
            this.lblGestureCount.Content = "Gesture Count: " + frame.Gestures().Count.ToString();
            this.lblImageCount.Content = "Image Count: " + frame.Images.Count.ToString();

            if (recordMode)
            {
                //write the recived frame to file
                writeFrameToFile(frame); //stimcode read from global

                
             

                //process image data from the frame
                Leap.Image image = frame.Images[0];
                if (image.Width != 0 && image.Height != 0)
                {
                    Bitmap bitmap = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                    //set palette
                    ColorPalette grayscale = bitmap.Palette;
                    for (int i = 0; i < 256; i++)
                    {
                        grayscale.Entries[i] = System.Drawing.Color.FromArgb((int)255, i, i, i);
                    }
                    bitmap.Palette = grayscale;
                    Rectangle lockArea = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                    BitmapData bitmapData = bitmap.LockBits(lockArea, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                    byte[] rawImageData = image.Data;
                    System.Runtime.InteropServices.Marshal.Copy(rawImageData, 0, bitmapData.Scan0, image.Width * image.Height);
                    bitmap.UnlockBits(bitmapData);

                    imgFrame.Source = ConvertBitmap.BitmapToBitmapSource(bitmap);
                }
                
                
            }
            
            

        }


        void writeFrameToFile(Leap.Frame frame)
        {
            //specific write method --> stores data in format: 64 bit serialised datetime object then 32 bit stimcode then 32 bit integer specifying size of frame, then serialised frame bytes of length (size of frame read before this) itself
            
            long binaryDate = DateTime.Now.ToBinary();

            Leap.Frame frameToSerialize = frame;
            byte[] serialized = frameToSerialize.Serialize;
            Int32 length = serialized.Length;

            
            Console.WriteLine(frame.Timestamp.ToString());
            writer.Write(binaryDate);
            writer.Write(curStimCode);
            writer.Write(length);
            writer.Write(serialized);

            //reset stim code to prevent multiple occurences of the stim event
            curStimCode = 0;


            
        }

        void MainWindow_Closing(object sender, EventArgs e)
        {
            this.isClosing = true;
            this.controller.RemoveListener(this.listener);
            this.controller.Dispose();
            this.writer.Close();
            this.writer.Dispose(); ;
        }

        private void btnRecord_Click(object sender, RoutedEventArgs e)
        {
            //create/overwrite frames file
            writer = new System.IO.BinaryWriter(System.IO.File.Open(tbFileName.Text.ToString(), System.IO.FileMode.Create));
            csvWriter = new System.IO.StreamWriter(System.IO.File.Open(tbCSVName.Text.ToString(), System.IO.FileMode.Create));

            recordMode = true;
            btnRecordPause.IsEnabled = true;
            btnRecord.IsEnabled = false;
            btnRecordStop.IsEnabled = true;
        }

        private void btnRecordStop_Click(object sender, RoutedEventArgs e)
        {
            recordMode = false;
            btnRecord.IsEnabled = true;
            btnRecordStop.IsEnabled = false;
            btnRecordResume.IsEnabled = false;
            btnRecordPause.IsEnabled = false;
            writer.Close();
        }
        private void btnRecordPause_Click(object sender, RoutedEventArgs e)
        {
            recordMode = false;
            btnRecordResume.IsEnabled = true;
            btnRecordPause.IsEnabled = false;
        }
        private void btnRecordResume_Click(object sender, RoutedEventArgs e)
        {
            recordMode = true;
            btnRecordResume.IsEnabled = false;
            btnRecordPause.IsEnabled = true;
        }

        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();



            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".data";
            dlg.Filter = "Frame data (*.data)|*.data";


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();


            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                using (System.IO.BinaryReader br =
                    new System.IO.BinaryReader(System.IO.File.Open(filename, System.IO.FileMode.Open)))
                {
                    while (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        //deserialises and reads the binary file
                        DateTime date = new DateTime();
                        date = DateTime.FromBinary(br.ReadInt64());
                       // Console.WriteLine(date.ToString());

                        Int32 stimCode = br.ReadInt32();
                       
                            

                        Int32 nextBlock = br.ReadInt32();
                        byte[] frameData = br.ReadBytes(nextBlock);
                        Leap.Frame newFrame = new Leap.Frame();
                        newFrame.Deserialize(frameData);

                        if (stimCode == 5443)
                        {
                            Debug.WriteLine("5443 detected: " + newFrame.Id);
                        }
                        frameList.Add(newFrame);
                        //Console.WriteLine(newFrame.CurrentFramesPerSecond);
                        
                    }
                    br.Close();
                    br.Dispose();
                }
                /* WRITE CODE HERE TO EXTRACT DATA FROM FILE
                foreach (Leap.Frame frame in frameList)
                {
                    //Console.WriteLine(frame.Id);
                }
                */
   
            }
        }

        private void btnStimCodeInject_Click(object sender, RoutedEventArgs e)
        {
            curStimCode = Convert.ToInt32(tbCurStimCode.Text);
        }

        

      

        
    }

    /// <summary>
    /// Interface that requires interfacer to have the function leapeventnotification that ac
    /// </summary>
    public interface ILeapEventDelegate
    {
        void LeapEventNotification(string EventName);
    }

    public class LeapEventListener : Listener
    {
        ILeapEventDelegate eventDelegate;

        public LeapEventListener(ILeapEventDelegate delegateObject)
        {
            this.eventDelegate = delegateObject;
        }
        public override void OnInit(Controller controller)
        {
            this.eventDelegate.LeapEventNotification("onInit");
        }
        public override void OnConnect(Controller controller)
        {
            controller.SetPolicy(Controller.PolicyFlag.POLICY_IMAGES);
            controller.EnableGesture(Gesture.GestureType.TYPE_SWIPE);
            this.eventDelegate.LeapEventNotification("onConnect");
        }

        public override void OnFrame(Controller controller)
        {
            this.eventDelegate.LeapEventNotification("onFrame");
        }
        public override void OnExit(Controller controller)
        {
            this.eventDelegate.LeapEventNotification("onExit");
        }
        public override void OnDisconnect(Controller controller)
        {
            this.eventDelegate.LeapEventNotification("onDisconnect");
        }

    }

    public class MyClient
    {
        /// <summary>
        /// Modified class to act as a named pipe server. Can add own function to subscribe to connection message event etc
        /// </summary>
        private NamedPipeClient<string> client;


        public MyClient(string pipeName)
        {
            this.client = new NamedPipeClient<string>(pipeName);
            client.ServerMessage += OnServerMessage;
            client.Error += OnError;
            client.Start();

        }

        private void OnServerMessage(NamedPipeConnection<string, string> connection, string message)
        {
            Debug.WriteLine("Server says: " + message);
            int value;
            if (int.TryParse(message, out value))
            {
                if (value > 0)
                    MainWindow.curStimCode = value;
                else
                    throw new ArgumentOutOfRangeException("Stimcode should only be positive");
            }
        }

        private void OnError(Exception exception)
        {
            Console.Error.WriteLine("ERROR: {0}", exception);
        }

        public void SendMessage(string message)
        {
            client.PushMessage(message);
        }

        public void Stop()
        {
            client.Stop();
        }
    }
}
