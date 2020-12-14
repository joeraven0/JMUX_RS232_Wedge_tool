using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Management;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace JMUX_RS232
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static CheckBox globalport0_checkbox = new CheckBox();
        public static CheckBox globalport1_checkbox = new CheckBox();
        public static CheckBox globalport2_checkbox = new CheckBox();
        public static TextBox globalport0_textbox = new TextBox();
        public static TextBox globalport1_textbox = new TextBox();
        public static TextBox globalport2_textbox = new TextBox();
        public static TextBox globalRawData = new TextBox();
        public static TextBox globalSplitterChar = new TextBox();
        public static int baud0, baud1, baud2;
        public static Queue<string> qt = new Queue<string>();
        public Thread sendKeysQueueThread;
        public static bool wedgeClicked = false;
        public static int fromPort;
        public static int suffixClicked = 0;
        public static string suffixChecked = "";
        public static bool splitterClicked = false;
        public static string splitterChars = "?";
        public static int splitTimerDelay = 0;
        private bool licenseButtonState = false;
        private bool licenseVal = false;
        public string deviceId = "";
        public string md5deviceId;
        private Thread licensing;
        static Mutex mutex = new Mutex(true, "{JMUXRS232Wedgetool}");
        public MainWindow()
        {
            
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                try
                {
                    Console.WriteLine("InstanceData ok!");
                    InitializeComponent();

                    InitializeRun();

                    licenseRequest();
                    rs232.InitializeComItems(port0_items);
                    rs232.InitializeComItems(port1_items);
                    rs232.InitializeComItems(port2_items);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
            else
            {
                MessageBox.Show("JMUX RS232 Wedge tool already running!");

                System.Environment.Exit(1);
            }



        }
        public void flushSettings()
        {
            deviceId = getUUID().ToString();
            Console.WriteLine("UUID: " + deviceId);
            licenseNo.Text = deviceId;

            md5deviceId = CreateMD5(deviceId);
            Properties.Settings.Default.wedgeClickedBool = wedgeClicked;
            Properties.Settings.Default.suffixClickedInt = suffixClicked;
            Properties.Settings.Default.splitterClickedBool = splitterClicked;
            Properties.Settings.Default.splitterCharContent = splitterChar.Text;
            Properties.Settings.Default.splitTimer = splitTimer.Text;


            if (suffixClicked == 1)
            {
                suffixChecked = "\n";
                this.Dispatcher.Invoke(() =>
                {
                    suffixBtn.FontSize = 18;
                    suffixBtn.Content = "<CR>";

                });
            }
            if (suffixClicked == 2)
            {
                suffixChecked = "\n\r";
                this.Dispatcher.Invoke(() =>
                {
                    suffixBtn.FontSize = 14;
                    suffixBtn.Content = "<CR><LF>";
                });
            }
            if (suffixClicked == 3)
            {
                suffixChecked = "\t";
                this.Dispatcher.Invoke(() =>
                {
                    suffixBtn.FontSize = 18;
                    suffixBtn.Content = "<TAB>";
                });
            }
            if (suffixClicked == 4)
            {
                suffixChecked = "\t\n";
                this.Dispatcher.Invoke(() =>
                {
                    suffixBtn.FontSize = 14;
                    suffixBtn.Content = "<TAB><CR>";
                });
            }
            if (suffixClicked == 5)
            {

                suffixChecked = "";
                this.Dispatcher.Invoke(() =>
                {
                    suffixBtn.Content = "Suffix";
                    suffixBtn.FontSize = 18;

                });
                suffixClicked = 0;
            }
            if (suffixClicked > 0)
            {
                suffixBtn.Background = Brushes.Orange;
                suffixBtn.FontStyle = FontStyles.Italic;
            }
            else
            {
                suffixBtn.Background = Brushes.LightGray;
                suffixBtn.FontStyle = FontStyles.Normal;
            }
            if (wedgeClicked)
            {
                wedgeEnableBtn.Background = Brushes.Orange;
                wedgeEnableBtn.FontStyle = FontStyles.Italic;
            }
            else
            {
                wedgeEnableBtn.Background = Brushes.LightGray;
                wedgeEnableBtn.FontStyle = FontStyles.Normal;
            }
            if (splitterClicked)
            {
                splitEnableBtn.Background = Brushes.Orange;
                splitEnableBtn.FontStyle = FontStyles.Italic;
                splitterChar.IsEnabled = false;
                splitTimer.IsEnabled = false;

            }
            else
            {
                splitEnableBtn.Background = Brushes.LightGray;
                splitEnableBtn.FontStyle = FontStyles.Normal;
                splitterChar.IsEnabled = true;
                splitTimer.IsEnabled = true;

            }
            Properties.Settings.Default.Save();
        }
        public void InitializeRun()
        {
            //Make em global
            port0_items.SelectedItem = Properties.Settings.Default.port0_selected;
            baud0_items.SelectedItem = Properties.Settings.Default.baud0_selected;
            port1_items.SelectedItem = Properties.Settings.Default.port1_selected;
            baud1_items.SelectedItem = Properties.Settings.Default.baud1_selected;
            port2_items.SelectedItem = Properties.Settings.Default.port2_selected;
            baud2_items.SelectedItem = Properties.Settings.Default.baud2_selected;
            splitTimer.Text = Properties.Settings.Default.splitTimer;
            globalport0_checkbox = port0_checkbox;
            globalport1_checkbox = port1_checkbox;
            globalport2_checkbox = port2_checkbox;
            globalport0_textbox = port0_text;
            globalport1_textbox = port1_text;
            globalport2_textbox = port2_text;
            globalSplitterChar = splitterChar;


            wedgeClicked = Properties.Settings.Default.wedgeClickedBool;
            suffixClicked = Properties.Settings.Default.suffixClickedInt;
            splitterClicked = Properties.Settings.Default.splitterClickedBool;
            splitterChars = Properties.Settings.Default.splitterCharContent;
            splitterChar.Text = Properties.Settings.Default.splitterCharContent;
            globalRawData = rawData;
            flushSettings();
            licenseRequest();
            sendKeysQueueThread = new Thread(sendKeysQueue);
            sendKeysQueueThread.Start();

            int[] baudrates = { 110, 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200, 128000, 256000 };
            foreach (int s in baudrates)
            {
                baud0_items.Items.Add(s);
                baud1_items.Items.Add(s);
                baud2_items.Items.Add(s);
            }
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fileVersionInfo.ProductName + " - " + PublishVersion;
            mainwindow_name.Title = version;
            
        }
        public string PublishVersion
        {
            get
            {
                if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
                {
                    Version ver = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;
                    return string.Format("{0}.{1}.{2}.{3}", ver.Major, ver.Minor, ver.Build, ver.Revision);
                }
                else
                    return "Not Published";
            }
        }
        public void sendKeysQueue()
        {
            
            while (true)
            {

               
                while (qt.Count > 0)
                {
                    while (qt.Count >= 5)
                    {

                        Console.WriteLine("Too much data. Queue cleaned");
                        qt.Dequeue();
                    }
                    Console.WriteLine("QT Count: " + qt.Count);
                    Console.WriteLine(qt.Peek());
                    Thread.Sleep(splitTimerDelay);
                    if (MainWindow.wedgeClicked&&qt.Count<5)
                    {
                        System.Windows.Forms.SendKeys.SendWait(qt.Peek());
                        Console.WriteLine(qt.Peek());
                        qt.Dequeue();

                    }

                    
                    
                }
            }
        }

        private void port0_checkbox_clicked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.autoconnectMaster = false;
            autoConnect_Check.IsChecked = false;
            connectPort0();

        }
        private void connectPort0()
        {
            if (port0_items.SelectedItem != null && baud0_items.SelectedItem != null)
            {
                fromPort = 0;
                rs232.ComPorts0(rs232.com0, port0_checkbox.IsChecked.Equals(true), port0_items.SelectedItem.ToString(), Int32.Parse(baud0_items.SelectedItem.ToString()));
                port0_items.IsEnabled = port0_checkbox.IsChecked.Equals(false);
                baud0_items.IsEnabled = port0_checkbox.IsChecked.Equals(false);
                Properties.Settings.Default.port0_selected = port0_items.SelectedItem.ToString();
                Properties.Settings.Default.baud0_selected = Int32.Parse(baud0_items.SelectedItem.ToString());
                Properties.Settings.Default.Save();
            }
            else
            {
                port0_checkbox.IsChecked = false;
            }
        }
        private void port1_checkbox_clicked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.autoconnectMaster = false;
            autoConnect_Check.IsChecked = false;
            connectPort1();
        }
        private void connectPort1()
        {
            if (port1_items.SelectedItem != null && baud1_items.SelectedItem != null)
            {
                fromPort = 1;
                rs232.ComPorts1(rs232.com1, port1_checkbox.IsChecked.Equals(true), port1_items.SelectedItem.ToString(), Int32.Parse(baud1_items.SelectedItem.ToString()));
                port1_items.IsEnabled = port1_checkbox.IsChecked.Equals(false);
                baud1_items.IsEnabled = port1_checkbox.IsChecked.Equals(false);
                Properties.Settings.Default.port1_selected = port1_items.SelectedItem.ToString();
                Properties.Settings.Default.baud1_selected = Int32.Parse(baud1_items.SelectedItem.ToString());
                Properties.Settings.Default.Save();
            }
            else
            {
                port1_checkbox.IsChecked = false;
            }
        }
        private void port2_checkbox_clicked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.autoconnectMaster = false;
            autoConnect_Check.IsChecked = false;
            connectPort2();
        }
        private void connectPort2()
        {
            if (port2_items.SelectedItem != null && baud2_items.SelectedItem != null)
            {
                fromPort = 2;
                rs232.ComPorts2(rs232.com2, port2_checkbox.IsChecked.Equals(true), port2_items.SelectedItem.ToString(), Int32.Parse(baud2_items.SelectedItem.ToString()));
                port2_items.IsEnabled = port2_checkbox.IsChecked.Equals(false);
                baud2_items.IsEnabled = port2_checkbox.IsChecked.Equals(false);
                Properties.Settings.Default.port2_selected = port2_items.SelectedItem.ToString();
                Properties.Settings.Default.baud2_selected = Int32.Parse(baud2_items.SelectedItem.ToString());
                Properties.Settings.Default.Save();
            }
            else
            {
                port2_checkbox.IsChecked = false;
            }
        }
        private void port0_item_selected(object sender, SelectionChangedEventArgs e)
        {
            port0_checkbox.Content = port0_items.SelectedItem;
            Console.WriteLine("Licenseval: " + licenseVal);
            if (licenseVal == true)
            {
                port0_checkbox.IsEnabled = true;
            }

        }
        private void port1_item_selected(object sender, SelectionChangedEventArgs e)
        {
            port1_checkbox.Content = port1_items.SelectedItem;

            port1_checkbox.IsEnabled = true & licenseVal;
        }
        private void port2_item_selected(object sender, SelectionChangedEventArgs e)
        {
            port2_checkbox.Content = port2_items.SelectedItem;
            port2_checkbox.IsEnabled = true & licenseVal;
        }

        private void port0_items_click(object sender, EventArgs e)
        {
            rs232.InitializeComItems(port0_items);
            port0_items.Items.Remove(port1_items.SelectedItem);
            port0_items.Items.Remove(port2_items.SelectedItem);
            port0_checkbox.Content = "Port 0";
            port0_checkbox.IsEnabled = false;
        }
        private void port1_items_click(object sender, EventArgs e)
        {
            rs232.InitializeComItems(port1_items);
            port1_items.Items.Remove(port0_items.SelectedItem);
            port1_items.Items.Remove(port2_items.SelectedItem);
            port1_checkbox.Content = "Port 1";
            port1_checkbox.IsEnabled = false;
        }

        private void baud0_selectionChanged(object sender, SelectionChangedEventArgs e)
        {
            baud0 = Int32.Parse(baud0_items.SelectedValue.ToString());
            Console.WriteLine("Baud0 selected: " + baud0.ToString());
        }
        private void baud1_selectionChanged(object sender, SelectionChangedEventArgs e)
        {
            baud1 = Int32.Parse(baud1_items.SelectedValue.ToString());
            Console.WriteLine("Baud1 selected: " + baud1.ToString());
        }
        private void baud2_selectionChanged(object sender, SelectionChangedEventArgs e)
        {
            baud2 = Int32.Parse(baud2_items.SelectedValue.ToString());
            Console.WriteLine("Baud2 selected: " + baud2.ToString());
        }

        private void suffixBtn_Click(object sender, RoutedEventArgs e)
        {

            suffixClicked++;
            flushSettings();

        }

        private void splitEnableClick(object sender, RoutedEventArgs e)
        {
            try
            {
                splitterClicked = !splitterClicked;
                splitterChars = splitterChar.Text;
                splitTimerDelay = Int32.Parse(splitTimer.Text);
                splitTimer.Background = Brushes.White;


            }
            catch
            {
                splitTimer.Background = Brushes.Red;
                splitTimer.Text = "NaN!";
            }
            flushSettings();

        }

        private void port2_items_click(object sender, EventArgs e)
        {
            rs232.InitializeComItems(port2_items);
            port2_items.Items.Remove(port0_items.SelectedItem);
            port2_items.Items.Remove(port1_items.SelectedItem);
            port2_checkbox.Content = "Port 2";
            port2_checkbox.IsEnabled = false;
        }

        private void wedgeBtn_Click(object sender, RoutedEventArgs e)
        {
            wedgeClicked = !wedgeClicked;
            flushSettings();
        }
        private void licenseBtn_Click(object sender, RoutedEventArgs e)
        {
            toggleLicenseWindow();

        }
        public static string getUUID()
        {
            string uuid = String.Empty;
            ManagementClass mc = new ManagementClass("Win32_ComputerSystemProduct");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc)
            {
                uuid = mo.Properties["UUID"].Value.ToString();
                break;
            }

            return uuid;


        }
        private void toggleLicenseWindow()
        {

            licenseButtonState = !licenseButtonState;
            if (licenseButtonState)
            {
                this.Dispatcher.Invoke(() =>
                {
                    licenseNo.Visibility = Visibility.Visible;
                    licenseNoBtn.Visibility = Visibility.Visible;
                    licenseBackground.Visibility = Visibility.Visible;
                    licenseComments.Visibility = Visibility.Visible;
                    copyDeviceId.Visibility = Visibility.Visible;
                    deviceidlabel.Visibility = Visibility.Visible;
                    mail.Visibility = Visibility.Visible;
                });
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    licenseNo.Visibility = Visibility.Hidden;
                    licenseNoBtn.Visibility = Visibility.Hidden;
                    licenseBackground.Visibility = Visibility.Hidden;
                    licenseComments.Visibility = Visibility.Hidden;
                    copyDeviceId.Visibility = Visibility.Hidden;
                    deviceidlabel.Visibility = Visibility.Hidden;
                    mail.Visibility = Visibility.Hidden;
                });
            }
        }
        private void licenseNoBtn_Click(object sender, RoutedEventArgs e)
        {
            licenseRequest();

        }
        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
        private void licenseRequest()
        {

            DateTime start = System.DateTime.Today;
            deviceId = getUUID();
            DateTime end = Properties.Settings.Default.licenseFlushExpires;
            int days = (end - start).Days;
            string licenseCommentsText = "";

            try
            {

                WebRequest request = WebRequest.Create(
                      "https://license.jmux.se/RS232Wedge/lic.tiu");
                // If required by the server, set the credentials.
                request.Credentials = CredentialCache.DefaultCredentials;

                // Get the response.
                WebResponse response = request.GetResponse();



                // Get the stream containing content returned by the server.
                // The using block ensures the stream is automatically closed.
                using (Stream dataStream = response.GetResponseStream())
                {

                    // Open the stream using a StreamReader for easy access.
                    StreamReader reader = new StreamReader(dataStream);
                    // Read the content.
                    string responseFromServer = reader.ReadToEnd().ToUpper();
                    // Display the content.
                    //Console.WriteLine(responseFromServer);
                    DateTime lastFlushed = Properties.Settings.Default.licenseFlushExpires;
                    Console.WriteLine("License flushed last time: " + lastFlushed.ToString());

                    Console.WriteLine("Days left: " + days.ToString());
                    string proLicense = "P" + md5deviceId;
                    string basicLicense = "B" + md5deviceId;
                    string blockLicense = "X" + md5deviceId;


                    if (responseFromServer.Contains(proLicense))
                    {
                        softwareVersion.Content = "Pro license";
                        licenseCommentsText = "Pro license is working properly";
                        licenseComments.Foreground = Brushes.Green;
                        Properties.Settings.Default.licenseLevel = 1;
                        Console.WriteLine("Pro license");
                        Console.WriteLine("LicenseLevel set to: " + Properties.Settings.Default.licenseLevel);



                        licenseLevels(1);
                        if (licensing == null)
                        {
                            licensing = new Thread(licenseRunner);
                            licensing.Start();

                        }
                        if (licensing != null && licensing.IsAlive == false)
                        {
                            licensing = new Thread(licenseRunner);
                            licensing.Start();
                        }


                        Properties.Settings.Default.licenseFlushExpires = System.DateTime.Today.AddDays(20);

                        this.Dispatcher.Invoke(() =>
                        {
                            port0_text.Text = "Pro version";
                            port1_text.Text = "Pro version";
                            port2_text.Text = "Pro version";
                            licenseNo.Background = Brushes.Green;
                            licenseBtn.Background = Brushes.LightGreen;

                        });



                        Properties.Settings.Default.licensetimer = 592000;
                        flushSettings();

                    }
                    else if (responseFromServer.Contains(basicLicense))
                    {
                        softwareVersion.Content = "Basic license ";
                        licenseCommentsText = "Basic license is working properly";
                        licenseComments.Foreground = Brushes.Green;
                        Properties.Settings.Default.licenseLevel = 2;
                        Console.WriteLine("Basic license");
                        Console.WriteLine("LicenseLevel set to: " + Properties.Settings.Default.licenseLevel);


                        licenseLevels(2);
                        if (licensing == null)
                        {
                            licensing = new Thread(licenseRunner);
                            licensing.Start();

                        }
                        else if (licensing != null && licensing.IsAlive == false)
                        {
                            licensing = new Thread(licenseRunner);
                            licensing.Start();
                        }


                        Properties.Settings.Default.licenseFlushExpires = System.DateTime.Today.AddDays(20);

                        this.Dispatcher.Invoke(() =>
                        {
                            port0_text.Text = "Basic version";
                            port1_text.Text = "";
                            port2_text.Text = "";
                            licenseNo.Background = Brushes.Green;
                            licenseBtn.Background = Brushes.LightGreen;
                        });
                        flushSettings();

                    }
                    else if (responseFromServer.Contains(blockLicense))
                    {
                        softwareVersion.Content = "No license";
                        licenseCommentsText = "No license available";
                        licenseComments.Foreground = Brushes.Red;
                        Properties.Settings.Default.licenseLevel = 0;
                        Console.WriteLine("Software license blocked");
                        Console.WriteLine("LicenseLevel set to: " + Properties.Settings.Default.licenseLevel);


                        licenseLevels(0);
                        if (licensing == null)
                        {
                            licensing = new Thread(licenseRunner);
                            licensing.Start();

                        }
                        if (licensing != null && licensing.IsAlive == false)
                        {
                            licensing = new Thread(licenseRunner);
                            licensing.Start();
                        }


                        Properties.Settings.Default.licenseFlushExpires = System.DateTime.Today.AddDays(0);

                        this.Dispatcher.Invoke(() =>
                        {
                            port0_text.Text = "No license";
                            port1_text.Text = "No license";
                            port2_text.Text = "No license";
                            licenseNo.Background = Brushes.Red;
                            licenseBtn.Background = Brushes.Red;
                        });



                        Properties.Settings.Default.licensetimer = 0;
                        flushSettings();
                    }
                    else if (days >= 0)
                    {

                        licenseBtn.Background = Brushes.Orange;

                        softwareVersion.Content = "No valid license - " + days + " days left";
                        licenseCommentsText = "License expired. Contact JMUX with Device ID for activation. \n" +
                    "The software will be deactivated in " + days + " days";

                        licenseComments.Foreground = Brushes.Orange;
                        if (Properties.Settings.Default.licenseLevel == 1)
                        {
                            licenseLevels(1);
                        }
                        else if (Properties.Settings.Default.licenseLevel == 2)
                        {
                            licenseLevels(2);
                        }









                        if (licensing == null)
                        {
                            licensing = new Thread(licenseRunner);
                            licensing.Start();

                        }
                        if (licensing != null && licensing.IsAlive == false)
                        {
                            licensing = new Thread(licenseRunner);
                            licensing.Start();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Does not contain =(");
                        licenseLevels(0);

                    }

                }


                // Close the response.
                response.Close();
            }
            catch (WebException)
            {
                Console.WriteLine("License error: Internet or server.");
                this.Dispatcher.Invoke(() =>
                {

                    licenseCommentsText = "Unable to verify license. " +
                    "Please connect to internet and flush license.\n" +
                    "The software will be deactivated in " + days + " days";


                    licenseComments.Foreground = Brushes.Red;

                    int licenseLeverer = Properties.Settings.Default.licenseLevel;
                    if (days > 0)
                    {



                        licenseBtn.Background = Brushes.Orange;

                        if (licenseLeverer == 1)
                        {
                            licenseLevels(1);

                            softwareVersion.Content = "Flush PRO license within " + days + " days";
                        }
                        if (licenseLeverer == 2)
                        {
                            licenseLevels(2);

                            softwareVersion.Content = "Flush BASIC license within: " + days + " days";
                        }
                        Console.WriteLine("Days still left but no license flush");
                        if (licensing == null)
                        {
                            licensing = new Thread(licenseRunner);
                            licensing.Start();

                        }
                        if (licensing != null && licensing.IsAlive == false)
                        {
                            licensing = new Thread(licenseRunner);
                            licensing.Start();
                        }






                    }
                    if (days <= 0)
                    {
                        licenseNo.Background = Brushes.Red;
                        licenseBtn.Background = Brushes.Red;
                        //licenseNo.Text = "Offline license overdue. Internet required";

                    }
                });

            }
            licenseComments.Document.Blocks.Clear();
            licenseComments.AppendText(licenseCommentsText);

        }

        private void copyDeviceId_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(deviceId);
            }
            catch { }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            flushSettings();

            if (licensing != null)
            {
                if (licensing.IsAlive == true)
                {
                    licensing.Abort();
                }
            }
            Console.WriteLine("Closed");
            if (sendKeysQueueThread != null)
            {
                sendKeysQueueThread.Abort();
            }
        }

        private void autoConnect_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.autoconnectMaster = autoConnect_Check.IsChecked.Equals(true);

            Properties.Settings.Default.autoconnect0 = port0_checkbox.IsChecked.Equals(true);
            Properties.Settings.Default.autoconnect1 = port1_checkbox.IsChecked.Equals(true);
            Properties.Settings.Default.autoconnect2 = port2_checkbox.IsChecked.Equals(true);
            Properties.Settings.Default.Save();





            //flushSettings();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            autoConnect_Check.IsChecked = Properties.Settings.Default.autoconnectMaster.Equals(true);
            if (Properties.Settings.Default.autoconnectMaster.Equals(true))
            {
                port0_checkbox.IsChecked = Properties.Settings.Default.autoconnect0;
                port1_checkbox.IsChecked = Properties.Settings.Default.autoconnect1;
                port2_checkbox.IsChecked = Properties.Settings.Default.autoconnect2;
                if (port0_checkbox.IsChecked.Equals(true))
                {
                    connectPort0();
                    Console.WriteLine("Tried to connect!!!");
                }
                if (port1_checkbox.IsChecked.Equals(true))
                {
                    connectPort1();
                }
                if (port2_checkbox.IsChecked.Equals(true))
                {
                    connectPort2();
                }
            }
        }

        private void licenseRunner()
        {
            int timer = Properties.Settings.Default.licensetimer;
            bool firstClick = true;
            while (false)
            {
                timer--;
                Thread.Sleep(1000);
                Console.WriteLine("Timer: " + timer.ToString());
                if ((timer > 5) && firstClick)
                {



                    firstClick = false;
                }

                else if (timer == 0)
                {
                    licenseLevels(0);

                    licensing.Abort();
                    break;
                }
            }
        }
        private void licenseLevels(int licenseLevel)
        {

            this.Dispatcher.Invoke(() =>
            {
                if (licenseLevel == 1)
                {
                    licenseVal = true;
                    port0_checkbox.IsEnabled = true;
                    port1_checkbox.IsEnabled = true;
                    port2_checkbox.IsEnabled = true;
                }
                if (licenseLevel == 2)
                {
                    licenseVal = true;
                    port0_checkbox.IsEnabled = true;
                    port1_checkbox.IsEnabled = false;
                    port2_checkbox.IsEnabled = false;




                }
                if (licenseLevel == 0)
                {
                    licenseNo.Background = Brushes.Red;
                    licenseVal = false;
                    port0_checkbox.IsEnabled = false;
                    port1_checkbox.IsEnabled = false;
                    port2_checkbox.IsEnabled = false;
                    licenseBtn.Background = Brushes.Red;

                    softwareVersion.Content = "License inactive";


                }
            });

            toggleLicenseWindow();


        }
    }
    public partial class rs232
    {
        public static SerialPort com0 = new SerialPort();
        public static SerialPort com1 = new SerialPort();
        public static SerialPort com2 = new SerialPort();



        public static void InitializeComItems(ComboBox portItems)
        {
            portItems.Items.Clear();
            foreach (string s in SerialPort.GetPortNames())
            {
                portItems.Items.Add(s);

            }
        }
        public static void ComPorts0(SerialPort myPort, bool comopener, string portname, int baud)
        {
            try
            {
                if (comopener)
                {
                    myPort.PortName = portname;
                    myPort.BaudRate = baud;
                    myPort.Open();
                    //myPort.WriteLine(myPort.PortName.ToString() + " Opened.\n");
                    Console.WriteLine(myPort.PortName.ToString() + " Opened.\n");
                    gui(0, "Connected", myPort.PortName, null);
                    myPort.DataReceived += new SerialDataReceivedEventHandler(readData0);

                }
                else
                {
                    //myPort.WriteLine(myPort.PortName.ToString() + " Closing.\n");
                    Console.WriteLine(myPort.PortName.ToString() + " Closing.\n");
                    myPort.Close();
                    gui(0, "disconnected", myPort.PortName, null);
                    myPort.DataReceived -= new SerialDataReceivedEventHandler(readData0);

                }
            }
            catch (System.UnauthorizedAccessException)
            {
                Console.WriteLine("Com already open: " + myPort.PortName);
                gui(0, "alreadyConnected", myPort.PortName, null);

            }
            catch (System.InvalidOperationException)
            {
                Console.WriteLine("Can't close someone elses com: " + myPort.PortName);
            }

        }
        public static void ComPorts1(SerialPort myPort, bool comopener, string portname, int baud)
        {
            try
            {
                if (comopener)
                {
                    myPort.PortName = portname;
                    myPort.BaudRate = baud;
                    
                    myPort.Open();
                    //myPort.WriteLine(myPort.PortName.ToString() + " Opened.\n");
                    Console.WriteLine(myPort.PortName.ToString() + " Opened.\n");
                    gui(1, "Connected", myPort.PortName, null);
                    myPort.DataReceived += new SerialDataReceivedEventHandler(readData1);
                }
                else
                {
                    //myPort.WriteLine(myPort.PortName.ToString() + " Closing.\n");
                    Console.WriteLine(myPort.PortName.ToString() + " Closing.\n");
                    myPort.Close();
                    gui(1, "disconnected", myPort.PortName, null);
                    myPort.DataReceived -= new SerialDataReceivedEventHandler(readData1);

                }
            }
            catch (System.UnauthorizedAccessException)
            {
                Console.WriteLine("Com already open: " + myPort.PortName);
                gui(1, "alreadyConnected", myPort.PortName, null);

            }
            catch (System.InvalidOperationException)
            {
                Console.WriteLine("Can't close someone elses com: " + myPort.PortName);
            }

        }
        public static void ComPorts2(SerialPort myPort, bool comopener, string portname, int baud)
        {
            try
            {
                if (comopener)
                {
                    myPort.PortName = portname;
                    myPort.BaudRate = baud;
                    myPort.Open();
                    //myPort.WriteLine(myPort.PortName.ToString() + " Opened.\n");
                    Console.WriteLine(myPort.PortName.ToString() + " Opened.\n");
                    gui(2, "Connected", myPort.PortName, null);
                    myPort.DataReceived += new SerialDataReceivedEventHandler(readData2);


                }
                else
                {
                    //myPort.WriteLine(myPort.PortName.ToString() + " Closing.\n");
                    Console.WriteLine(myPort.PortName.ToString() + " Closing.\n");
                    myPort.Close();
                    gui(2, "disconnected", myPort.PortName, null);
                    myPort.DataReceived -= new SerialDataReceivedEventHandler(readData2);

                }
            }
            catch (System.UnauthorizedAccessException)
            {
                Console.WriteLine("Com already open: " + myPort.PortName);
                gui(2, "alreadyConnected", myPort.PortName, null);

            }
            catch (System.InvalidOperationException)
            {
                Console.WriteLine("Can't close someone elses com: " + myPort.PortName);
            }

        }


        private static void readData0(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string indata = "";
                int byteData = 0;
                string byteDataString = "";

                Byte[] data = new Byte[256];
                
                while (com0.BytesToRead > 0)
                {
                    
                    byteData = com0.ReadByte();
                    byteDataString += byteData.ToString("X2") + " ";
                    indata += Convert.ToChar(byteData);
                }
                if (indata.Length>60) {
                    Console.WriteLine("indata cleared, too long: "+indata.Length);
                    indata = "";
                    byteDataString = "Data exceed 60 bytes.";
                }
                string presentationData = indata;
                if (presentationData.Length > 19)
                {
                    presentationData = indata.Substring(0, 7) + "..." + indata.Substring(presentationData.Length - 7, 7);
                    Console.WriteLine("PresentationData: " + presentationData);
                }

                rs232.gui(0, "receivedData", com0.PortName, presentationData);
                rs232.gui(99, "receivedDataHex", com0.PortName, byteDataString);
                Console.WriteLine(byteDataString);
                Console.WriteLine("readData: " + indata);

                bool splitterExist = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    splitterExist = MainWindow.globalSplitterChar.Text.Length > 0;

                });
                if (MainWindow.splitterClicked == true && splitterExist)
                {
                    string[] words = indata.Split(MainWindow.splitterChars[0]);
                    foreach (var word in words)
                    {
                        System.Console.WriteLine($"<{word}>");

                        if (MainWindow.wedgeClicked && word.Length < 60)
                        {
                            MainWindow.qt.Enqueue(word + MainWindow.suffixChecked);

                        }                        
                    }
                }
                else
                {
                    if (MainWindow.wedgeClicked)
                    {
                        MainWindow.qt.Enqueue(indata+MainWindow.suffixChecked);
                    }
                }
            }
            catch { }




        }
        private static void readData1(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string indata = "";
                int byteData = 0;
                string byteDataString = "";

                Byte[] data = new Byte[256];
                while (com1.BytesToRead > 0)
                {
                    byteData = com1.ReadByte();
                    byteDataString += byteData.ToString("X2") + " ";
                    indata += Convert.ToChar(byteData);
                }
                if (indata.Length > 60)
                {
                    Console.WriteLine("indata cleared, too long: " + indata.Length);
                    indata = "";
                    byteDataString = "Data exceed 60 bytes.";
                }
                string presentationData = indata;
                if (presentationData.Length > 19)
                {
                    presentationData = indata.Substring(0, 7) + "..." + indata.Substring(presentationData.Length - 7, 7);
                    Console.WriteLine("PresentationData: " + presentationData);
                }
                rs232.gui(1, "receivedData", com1.PortName, presentationData);
                rs232.gui(99, "receivedDataHex", com1.PortName, byteDataString);
                Console.WriteLine(byteDataString);
                Console.WriteLine("readData: " + indata);

                bool splitterExist = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    splitterExist = MainWindow.globalSplitterChar.Text.Length > 0;

                });
                if (MainWindow.splitterClicked == true && splitterExist)
                {
                    string[] words = indata.Split(MainWindow.splitterChars[0]);
                    foreach (var word in words)
                    {
                        System.Console.WriteLine($"<{word}>");
                        if (MainWindow.wedgeClicked)
                        {
                            MainWindow.qt.Enqueue(word + MainWindow.suffixChecked);

                        }
                    }
                }
                else
                {
                    if (MainWindow.wedgeClicked)
                    {
                        MainWindow.qt.Enqueue(indata + MainWindow.suffixChecked);
                    }
                }
            }
            catch { }

        }
        private static void readData2(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string indata = "";
                int byteData = 0;
                string byteDataString = "";

                Byte[] data = new Byte[256];
                while (com2.BytesToRead > 0)
                {
                    byteData = com2.ReadByte();
                    byteDataString += byteData.ToString("X2") + " ";
                    indata += Convert.ToChar(byteData);
                }
                if (indata.Length > 60)
                {
                    Console.WriteLine("indata cleared, too long: " + indata.Length);
                    indata = "";
                    byteDataString = "Data exceed 60 bytes.";
                }
                string presentationData = indata;
                if (presentationData.Length > 19)
                {
                    presentationData = indata.Substring(0, 7) + "..." + indata.Substring(presentationData.Length - 7, 7);
                    Console.WriteLine("PresentationData: " + presentationData);
                }
                rs232.gui(2, "receivedData", com2.PortName, presentationData);
                rs232.gui(99, "receivedDataHex", com2.PortName, byteDataString);
                Console.WriteLine(byteDataString);
                Console.WriteLine("readData: " + indata);

                bool splitterExist = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    splitterExist = MainWindow.globalSplitterChar.Text.Length > 0;

                });
                if (MainWindow.splitterClicked == true && splitterExist)
                {
                    string[] words = indata.Split(MainWindow.splitterChars[0]);
                    foreach (var word in words)
                    {
                        System.Console.WriteLine($"<{word}>");
                        if (MainWindow.wedgeClicked)
                        {
                            MainWindow.qt.Enqueue(word + MainWindow.suffixChecked);

                        }
                    }
                }
                else
                {
                    if (MainWindow.wedgeClicked)
                    {
                        MainWindow.qt.Enqueue(indata + MainWindow.suffixChecked);
                    }
                }
            }
            catch { }
        }
        public static void gui(int portNo, string category, string PortName, string msg)
        {
            int fromPort = portNo;
            if (category == "Connected")
            {

                if (fromPort == 0)
                {

                    MainWindow.globalport0_textbox.Text = PortName + " connected!";
                    MainWindow.globalport0_textbox.Foreground = Brushes.Green;
                }
                else if (fromPort == 1)
                {
                    MainWindow.globalport1_textbox.Text = PortName + " connected!";
                    MainWindow.globalport1_textbox.Foreground = Brushes.Green;
                }
                else if (fromPort == 2)
                {
                    MainWindow.globalport2_textbox.Text = PortName + " connected!";
                    MainWindow.globalport2_textbox.Foreground = Brushes.Green;
                }
            }
            if (category == "disconnected")
            {
                if (fromPort == 0)
                {
                    MainWindow.globalport0_textbox.Text = PortName + " disconnected.";
                    MainWindow.globalport0_textbox.Foreground = Brushes.Black;
                }
                else if (fromPort == 1)
                {
                    MainWindow.globalport1_textbox.Text = PortName + " disconnected.";
                    MainWindow.globalport1_textbox.Foreground = Brushes.Black;
                }
                else if (fromPort == 2)
                {
                    MainWindow.globalport2_textbox.Text = PortName + " disconnected.";
                    MainWindow.globalport2_textbox.Foreground = Brushes.Black;
                }
            }
            if (category == "alreadyConnected")
            {
                if (fromPort == 0)
                {
                    MainWindow.globalport0_checkbox.IsChecked = false;
                    MainWindow.globalport0_textbox.Text = PortName + " already open.";
                    MainWindow.globalport0_textbox.Foreground = Brushes.Red;
                }
                else if (fromPort == 1)
                {
                    MainWindow.globalport1_checkbox.IsChecked = false;
                    MainWindow.globalport1_textbox.Text = PortName + " already open.";
                    MainWindow.globalport1_textbox.Foreground = Brushes.Red;
                }
                else if (fromPort == 2)
                {
                    MainWindow.globalport2_checkbox.IsChecked = false;
                    MainWindow.globalport2_textbox.Text = PortName + " already open.";
                    MainWindow.globalport2_textbox.Foreground = Brushes.Red;
                }
            }
            if (category == "receivedData")
            {
                if (fromPort == 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MainWindow.globalport0_textbox.Text = msg;
                        MainWindow.globalport0_textbox.Foreground = Brushes.Black;
                    });
                }
                else if (fromPort == 1)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MainWindow.globalport1_textbox.Text = msg;
                        MainWindow.globalport1_textbox.Foreground = Brushes.Black;
                    });
                }
                else if (fromPort == 2)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MainWindow.globalport2_textbox.Text = msg;
                        MainWindow.globalport2_textbox.Foreground = Brushes.Black;
                    });
                }



            }
            if (category == "receivedDataHex")
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow.globalRawData.Text = msg;
                });
            }

        }
        public static void printInvoke(int fromPort, string msg)
        {

            if (fromPort == 0) { MainWindow.globalport0_textbox.Text = msg; }


        }
    }
}
