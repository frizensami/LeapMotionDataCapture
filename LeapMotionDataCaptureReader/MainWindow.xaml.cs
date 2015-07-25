using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LeapMotionDataCaptureReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Leap.Frame> frameList = new List<Leap.Frame>();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnChooseFile_Click(object sender, RoutedEventArgs e)
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
                frameList.Clear();
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (var frame in frameList)
            {
                PrintProperties(frame);
            }
        }

        public void PrintProperties(Leap.Frame frame)
        {
            //add code to print data from frame here
            //e.g. Console.WriteLine(frame.Fingers.Frontmost.Direction.X)
        }
    }
}
