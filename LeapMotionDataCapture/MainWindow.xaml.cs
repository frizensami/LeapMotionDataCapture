using System;
using System.Collections.Generic;
using System.Windows;
using Leap;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.IO;

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
        
        //var to check if recording is requested
        private Boolean recordMode = false;

        //list to hold all deserialised frames
        List<Leap.Frame> frameList;

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

            
            
            //init the frame list
            frameList = new List<Leap.Frame>();

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
                writeFrameToFile(frame);
                
                //process image data from the frame
                Leap.Image image = frame.Images[0];
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

        void writeFrameToFile(Leap.Frame frame)
        {
            //specific write method --> stores data in format: 4 byte integer specifying size of frame followed by frame bytes themselves
            Leap.Frame frameToSerialize = frame;
            byte[] serialized = frameToSerialize.Serialize;
            Int32 length = serialized.Length;
            writer.Write(length);
            writer.Write(serialized);
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
                        Int32 nextBlock = br.ReadInt32();
                        byte[] frameData = br.ReadBytes(nextBlock);
                        Leap.Frame newFrame = new Leap.Frame();
                        newFrame.Deserialize(frameData);
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
}
