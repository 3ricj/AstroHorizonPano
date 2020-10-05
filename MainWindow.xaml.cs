using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32; 
using ASCOM.Astrometry;
using ASCOM.Utilities;
using ChoETL;
using System.Text.RegularExpressions;
using System.Xml.Xsl;
using System.Xml;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using GeoTimeZone;
using TimeZoneConverter;
using System.Diagnostics;

namespace AstroHorizonPano
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class Position
    {
        public string panelname { get; set; }
        public double RA_decimal { get; set; }
        public int RA_hr { get; set; }
        public int RA_min { get; set; }
        public int RA_sec { get; set; }
        public double Dec_decimal { get; set; }

    }
    public class ProcessedPositions
    {
        public int ID { get; set; }
        public string PanelName { get; set; }
        public double RA_decimal { get; set; }
        public double Dec_decimal { get; set; }
        public DateTime TimeShot { get; set; }
        public double Alt { get; set; }
        public double Az { get; set; }
    }
    public partial class MainWindow : Window
    {
        private string[] files;
        private string OutputXMLFile;
        // Global info on sequence
        public double SiteLat;
        public double SiteLong; 
        //public double StartMJD;
        public double StartJulian;
        public TimeZoneInfo SequenceTimeZone;
        public TimeSpan  SequenceTimeZoneOffset; 


        ASCOM.Astrometry.Transform.Transform transform = new ASCOM.Astrometry.Transform.Transform();
        Util utility = new ASCOM.Utilities.Util();
        ASCOM.Astrometry.AstroUtils.AstroUtils apUtil = new ASCOM.Astrometry.AstroUtils.AstroUtils();


        public MainWindow()
        {
            InitializeComponent();
            SetupSequenceComboBoxes();

            Lat.TextChanged += PositionChangedEventHandler;
            Long.TextChanged += PositionChangedEventHandler;
            SequenceStartDateTimePicker.SelectedDateChanged += SequenceStartDateChangedEventHandler;
            SequenceStartHour.SelectionChanged += SequenceStartDateChangedEventHandler;
            SequenceStartMinute.SelectionChanged += SequenceStartDateChangedEventHandler;

            UpdateAutoTimeZone();


        }
        private void SetupSequenceComboBoxes()
        {

            
            foreach (int hour in Enumerable.Range(0, 24).ToList())
            {
                SequenceStartHour.Items.Add(hour.ToString());
            }
            foreach (int min in Enumerable.Range(0, 60).ToList())
            {
                SequenceStartMinute.Items.Add(min.ToString());
            }


            //ReadOnlyCollection<TimeZoneInfo> timeZones = TimeZoneInfo.GetSystemTimeZones();
            //int timezoneIndex = 0;


            //foreach (TimeZoneInfo timeZoneInfo in timeZones)
           // {
             //   TimeZoneComboBox.Items.Add(timeZoneInfo.DisplayName);
//                TimeZoneInfo.
                 //   Console.WriteLine("{0}, {0}", timeZoneInfo.DisplayName, timeZoneInfo.Id);
            //}
            //TimeZoneComboBox.SelectedIndex = 10;
                //Console.WriteLine("{0}", timeZoneInfo.DisplayName);
            //            SequenceStartHour.Items.Add("0");

        }
        private TimeSpan GetTZOffset(DateTime time, TimeZoneInfo timeZone)
        {
            TimeSpan offset;
            offset = timeZone.GetUtcOffset(time);
            Console.WriteLine("TimeZone Offset: " + offset.TotalHours);
            return offset;
        }
        public void UpdateAutoTimeZone()
        {
            try
            {
                var tzIana = TimeZoneLookup.GetTimeZone(Convert.ToDouble(Lat.Text), Convert.ToDouble(Long.Text)).Result;
                var tzMs = TZConvert.IanaToWindows(tzIana);
                var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tzMs);
                SequenceTimeZone = tzInfo;
                //SequenceTimeZoneOffset = TimeZoneInfo.GetUtcOffset;
                SequenceTimeZoneOffset = GetTZOffset(DateTime.Now, tzInfo);
                //                SequenceTimeZoneOffset = TimeZoneInfo.
                //TimeZoneInfo.FindSystemTimeZoneById(tzMs);
                //offsetFromUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.UtcNow, tzInfo);
                //Console.WriteLine("{0}", TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo));
                LocalTimeNowTextBox.Text = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo).ToString();
                var convertedTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);
                TimeZoneTextBlock.Text = tzMs.ToString();
                SiteLat = Convert.ToDouble(Lat.Text);
                SiteLong = Convert.ToDouble(Long.Text);

            }
            catch { TimeZoneTextBlock.Text = "INVALID"; }
        }
        public void UpdateEnvironmentals()
        {
            //StartMJD = 
            var utils = new ASCOM.Astrometry.AstroUtils.AstroUtils();
            DateTime startdate = SequenceStartDateTimePicker.SelectedDate.Value.Date;
            if (SequenceStartHour.SelectedIndex > -1) { startdate = startdate.AddHours(((double)SequenceStartHour.SelectedIndex)); }
            if (SequenceStartMinute.SelectedIndex > -1) { startdate = startdate.AddMinutes((double)SequenceStartMinute.SelectedIndex); }

//            Console.WriteLine("Local: {0}", startdate);

            string MoonInfo = "";
            string SunInfo = "";
            string AstroInfo = "";


            //StartMJD = utils.CalendarToMJD(startdate.Day, startdate.Month, startdate.Year);
            //julian  utility.DateLocalToJulian(startdate);
            StartJulian = utility.DateLocalToJulian(startdate); 

            //if (SequenceStartHour.SelectedIndex > -1) { StartMJD = StartMJD + ((double) SequenceStartHour.SelectedIndex / 24); }
            //if (SequenceStartMinute.SelectedIndex > -1) { StartMJD = StartMJD + ((double)SequenceStartMinute.SelectedIndex / 1440); }

            //Console.WriteLine("{0}", StartMJD);
            Console.WriteLine(" Illumination: {0}", utils.MoonIllumination(StartJulian));
            MoonInfo = utils.MoonIllumination(StartJulian).ToString("p") + " Illumination      \n";

            //ASCOM.Astrometry.AstroUtils.AstroUtils apUtil = new ASCOM.Astrometry.AstroUtils.AstroUtils();
            System.Collections.ArrayList astroNight = new System.Collections.ArrayList();
            System.Collections.ArrayList civilNight = new System.Collections.ArrayList();
            System.Collections.ArrayList sunRiseSet = new System.Collections.ArrayList();
            System.Collections.ArrayList MoonRiseSet = new System.Collections.ArrayList();
            //DateTime lastUpdate = TimeZoneInfo.ConvertTimeToUtc(startdate, SequenceTimeZone);
            DateTime lastUpdate = startdate;

            double lat = SiteLat;
            double lon = SiteLong;
            civilNight = apUtil.EventTimes(ASCOM.Astrometry.EventType.CivilTwilight, lastUpdate.Day, lastUpdate.Month, lastUpdate.Year, lat, lon, SequenceTimeZoneOffset.TotalHours);
            astroNight = apUtil.EventTimes(ASCOM.Astrometry.EventType.AstronomicalTwilight, lastUpdate.Day, lastUpdate.Month, lastUpdate.Year, lat, lon, SequenceTimeZoneOffset.TotalHours);
            sunRiseSet = apUtil.EventTimes(ASCOM.Astrometry.EventType.SunRiseSunset, lastUpdate.Day, lastUpdate.Month, lastUpdate.Year, lat, lon, SequenceTimeZoneOffset.TotalHours);
            MoonRiseSet = apUtil.EventTimes(ASCOM.Astrometry.EventType.MoonRiseMoonSet, lastUpdate.Day, lastUpdate.Month, lastUpdate.Year, lat, lon, SequenceTimeZoneOffset.TotalHours);
            

//            Console.WriteLine("SunSet: " + TimeSpan.FromHours((double)(sunRiseSet[4])).ToString("h\\:mm"));

            if (sunRiseSet.Count > 0)
            {
                int NumberOfRises = ((int)sunRiseSet[1]); //' Retrieve the number of sunsets
                int NumberOfSets = ((int)sunRiseSet[2]); //' Retrieve the number of sunsets

                for (int sets = 0; sets < NumberOfRises; sets++)
                {
                    if ((double)(sunRiseSet[sets + 3]) > (double)(sunRiseSet[sets + 4]))
                    {
                        SunInfo += " Sunset: " + TimeSpan.FromHours((double)(sunRiseSet[sets + 4])).ToString("h\\:mm");
                        SunInfo += " Sunrise: " + TimeSpan.FromHours((double)(sunRiseSet[sets + 3])).ToString("h\\:mm");
                    }
                    else
                    {
                        SunInfo += " Sunrise: " + TimeSpan.FromHours((double)(sunRiseSet[sets + 3])).ToString("h\\:mm");
                        SunInfo += " Sunset: " + TimeSpan.FromHours((double)(sunRiseSet[sets + 4])).ToString("h\\:mm");
                    }
                }

            }
            Console.WriteLine("Suninfo : " + SunInfo);

            if (MoonRiseSet.Count > 0)
            {
                int NumberOfRises = ((int)MoonRiseSet[1]); //' Retrieve the number of sunsets
                int NumberOfSets = ((int)MoonRiseSet[2]); //' Retrieve the number of sunsets
                for (int sets = 0; sets < NumberOfRises; sets++)
                {
                    if ((double)(MoonRiseSet[sets + 3]) > (double)(MoonRiseSet[sets + 4]))
                    {
                        MoonInfo += " Moonset: " + TimeSpan.FromHours((double)(MoonRiseSet[sets + 4])).ToString("h\\:mm");
                        MoonInfo += " Moonrise: " + TimeSpan.FromHours((double)(MoonRiseSet[sets + 3])).ToString("h\\:mm");
                    }
                    else
                    {
                        MoonInfo += " Moonrise: " + TimeSpan.FromHours((double)(MoonRiseSet[sets + 3])).ToString("h\\:mm");
                        MoonInfo += " Moonset: " + TimeSpan.FromHours((double)(MoonRiseSet[sets + 4])).ToString("h\\:mm");
                    }
                }
                //MoonInfo += TimeSpan.FromHours((double)(MoonRiseSet[NumberOfSets + 3])).ToString("h\\:mm");
            }
            Console.WriteLine("Mooninfo: " + MoonInfo);

            if (astroNight.Count > 0)
            {
                int NumberOfRises = ((int)astroNight[1]); //' Retrieve the number of sunsets
                int NumberOfSets = ((int)astroNight[2]); //' Retrieve the number of sunsets
                for (int sets = 0; sets < NumberOfRises; sets++)
                {
                    if ((double)(astroNight[sets + 3]) > (double)(astroNight[sets + 4]))
                    {
                        AstroInfo += " AstroStart: " + TimeSpan.FromHours((double)(astroNight[sets + 4])).ToString("h\\:mm");
                        AstroInfo += " AstroEnd: " + TimeSpan.FromHours((double)(astroNight[sets + 3])).ToString("h\\:mm");
                    }
                    else
                    {
                        AstroInfo += " AstroEnd: " + TimeSpan.FromHours((double)(astroNight[sets + 3])).ToString("h\\:mm");
                        AstroInfo += " AstroStart: " + TimeSpan.FromHours((double)(astroNight[sets + 4])).ToString("h\\:mm");
                    }
                }
                //MoonInfo += TimeSpan.FromHours((double)(MoonRiseSet[NumberOfSets + 3])).ToString("h\\:mm");
            }
            Console.WriteLine("AstroInfo: " + AstroInfo);

            //Console.WriteLine("Event Times 1: " + EventTimes1);
            //Console.WriteLine("Event Times 2: " + EventTimes2);

            EnvironmentalTextBlock.Text = SunInfo + "\n" + AstroInfo;
            MoonInfoTextBlock.Text = MoonInfo; 

            /*
            Console.WriteLine("Latitude: " + lat.ToString());
            Console.WriteLine("Longitude:" + lon.ToString());
            Console.WriteLine("Time" + lastUpdate.ToString());


            Console.WriteLine("sunRiseSet cont :" + sunRiseSet.Count.ToString());
            foreach (var e in sunRiseSet) Console.WriteLine("Array sunRiseSet:" + e.ToString());
            Console.WriteLine("civilNignht cont :" + civilNight.Count.ToString());
            foreach (var e in astroNight) Console.WriteLine("Array astro:" + e.ToString());

            Console.WriteLine("astroNight cont :" + astroNight.Count.ToString());

            DateTime objDateWoHour = lastUpdate.Date;*/
        }

        private void SequenceStartDateChangedEventHandler(object sender, SelectionChangedEventArgs args)
        {
            DateTime? selectedDate = SequenceStartDateTimePicker.SelectedDate;
            if (selectedDate.HasValue) { UpdateEnvironmentals(); }
        }
        private void PositionChangedEventHandler(object sender, TextChangedEventArgs args)
        {
            //if (String.IsNullOrEmpty(Lat.Text) || String.IsNullOrEmpty(Long.Text)) return;
                UpdateAutoTimeZone(); 
                
        }
        public (double  Alt, double Az) ConvertAltAz(double RaHours, double DecHours , double JuliuanDate)
        {


            transform.SiteLatitude = Convert.ToDouble(Lat.Text);
            transform.SiteLongitude = Convert.ToDouble(Long.Text);
            transform.SiteElevation = Convert.ToDouble(Elevation.Text);
            transform.SiteTemperature = 20.0;
            transform.JulianDateTT = JuliuanDate;
            

            //T.SetJ2000(U.HMSToHours("14:15:38.951"), U.DMSToDegrees("19:10:38.06"));
            transform.SetJ2000(RaHours, DecHours);
            //Console.WriteLine("RA: " + RaHours.ToString() + " DEC: " + DecHours.ToString());

            //MsgBox("RA Topo: " & U.HoursToHMS(T.RATopocentric, ":", ":", "", 3) & " DEC Topo: " & U.DegreesToDMS(T.DECTopocentric, ":", ":", "", 3))
            Console.WriteLine("Azimuth: " + transform.AzimuthTopocentric.ToString() + " Elevation: "  + transform.ElevationTopocentric.ToString());

            return (transform.ElevationTopocentric, transform.AzimuthTopocentric);

            //'Set arbitary topocentric co-ordinates and read off the corresponding J2000 co-ordinates (site parameters remain the same)
            //T.SetTopocentric(11.0, 25.0);
        //MsgBox("RA J2000: " & U.HoursToHMS(T.RAJ2000, ":", ":", "", 3) & " DEC J2000: " & U.DegreesToDMS(T.DecJ2000, ":", ":", "", 3))

            //'Clean up components

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            ofd.CheckFileExists = true;
            ofd.Filter = "Comma Delimited Files (*.csv) | *.csv";
            ofd.Title = "Select the CSV File";
            ofd.Multiselect = false;

            if (ofd.ShowDialog() == true)
                InputFileTextBox.Text = ofd.FileName;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (InputFileTextBox.Text == "") { return; }
            string InputText = System.IO.File.ReadAllText(InputFileTextBox.Text);
            //Console.WriteLine(InputText);
            InputText = Regex.Replace(InputText, ":", "_");
            InputText = Regex.Replace(InputText, "\"", "");


            //double JulianDate = StartMJD + 2400000.5;

            List<ProcessedPositions> processedPositions = new List<ProcessedPositions>();
//            processedPositions.Add(new ProcessedPositions() { ID = 0, PanelName = "test" });
            double Alt;
            double Az;
            int count = 0;

            foreach (var rec in ChoCSVReader<Position>.LoadText(InputText).WithFirstLineHeader())
            {
                Console.WriteLine($"panelname: " + rec.panelname.ToString());
                Console.WriteLine($"RA_decimal: {rec.RA_decimal}");
                Console.WriteLine($"Dec_decimal: {rec.Dec_decimal}");
                (Alt, Az) = ConvertAltAz(rec.RA_decimal, rec.Dec_decimal, StartJulian );
                
                processedPositions.Add(new ProcessedPositions() { ID = count, PanelName = rec.panelname.ToString(), Dec_decimal= rec.Dec_decimal, RA_decimal= rec.RA_decimal, Alt=Alt, Az=Az, TimeShot=utility.DateJulianToLocal(StartJulian) });

                StartJulian = StartJulian + (Convert.ToDouble(MinutesPerShotTextBox.Text) / 1440);
                count++;

            }
            OutputDataGrid.ItemsSource = processedPositions;
            UpdateAutoTimeZone();

            StringBuilder sb = new StringBuilder();
            using (var p = ChoCSVReader.LoadText(InputText).WithFirstLineHeader())
            {
                //    Console.WriteLine(p.ToString());
                using (var w = new ChoXmlWriter(sb)
                .Configure(c => c.RootName = "Mosaic")
                .Configure(c => c.NodeName = "Position")
                )
                    w.Write(p);
                
            }
            //Console.WriteLine(sb.ToString());

            //InputXML = 
            /*
            try
            {

                StringReader SR_InputXML = new StringReader(sb.ToString());
                XmlReader xr = XmlReader.Create(SR_InputXML);

                XslCompiledTransform myXSLT;
                myXSLT = new XslCompiledTransform();
                //                myXSLT.Load(System.IO.Directory.GetCurrentDirectory() + "\\" + TemplateComboBox.SelectedItem.ToString());
                myXSLT.Load(files[TemplateComboBox.SelectedIndex]);

                //myXSLT.Load('C:\Users\3ricj\source\repos\PositionTransmogrifier\bin\Debug\QuickShot.xslt');
                XmlTextWriter myWriter = new XmlTextWriter(OutputXMLFile, null);
                myXSLT.Transform(xr, null, myWriter);
            }
            catch { MessageBox.Show("Error performing conversion"); }

            MessageBox.Show("Converted file: " + OutputXMLFile);

            //System.Windows.Application.Current.Shutdown();
            */
        }
    }
}
