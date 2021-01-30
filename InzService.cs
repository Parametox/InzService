<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Net.Mqtt;
using System.Collections;

namespace InzService
{
    public partial class InzService : ServiceBase
    {
        // => deklaracje i inicjalizacje
        Stopwatch Stopwatch;
        private EventLog eventLog1;
        private int eventId = 1;

        private string mqttBroker1 = "mqtt.eclipse.org";
        private string mqttBroker2 = "test.mosquitto.org";
        private string clientId = Guid.NewGuid().ToString();

        private string temperature;
        private string address;
        private string fanStatus;
        SessionState ss1, ss2;
        IMqttClient client, client2;
        DateTime StartDate = DateTime.Now;
        // <=

        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        /// <summary>
        /// Konstruktor
        /// </summary>
        public InzService()
        {
            InitializeComponent();
            eventLog1 = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("InzServiceSource"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "InzServiceSource", "InzServiceLog");
            }
            eventLog1.Source = "InzServiceSource";
            eventLog1.Log = "InzServiceLog";

            // => MQTT
            this.InitBroker();
            // <MQTT
        }

        /// <summary>
        /// Inicjalizacja brokera MQTT
        /// </summary>
        private async void InitBroker()
        {
            MqttConfiguration config = new MqttConfiguration();

            Stopwatch sw = new Stopwatch();


            try
            {
                sw.Start(); // uruchomienie zegara
                client = await MqttClient.CreateAsync(mqttBroker1, config);
                ss1 = await client.ConnectAsync(new MqttClientCredentials(clientId: this.clientId.Replace("-", String.Empty))); // połaczenie do brokera z indywidualnym identyfikatorem
                await client.SubscribeAsync("pcipcipci", MqttQualityOfService.AtMostOnce);  // subskrybcja tematu "pcipcipci"
                client.MessageStream.Subscribe(msg => returnFormMqtt(msg.Topic, msg.Payload)); // Akcja wskazująca obsługę przychodzącej wiadomości przez wpisaną metodę
                sw.Stop();  //zatrzymanie zegara
                CreateLog("Init Broker client1", $"OK time {sw.ElapsedMilliseconds} ms."); // insert do bazy danych
            }
            catch (Exception ex)
            {
                sw.Stop();
                CreateLog("Init Broker1 Exception", $"{ex.Message} time {sw.ElapsedMilliseconds} [ms]");
            }

            // powtórzenie czynności dla drugiego brokera(aletrnatywnego)
            try
            {
                sw.Start();
                MqttConfiguration config2 = new MqttConfiguration();
                client2 = await MqttClient.CreateAsync(mqttBroker2, config2);
                ss2 = await client2.ConnectAsync(new MqttClientCredentials(clientId: this.clientId.Replace("-", String.Empty)));
                await client2.SubscribeAsync("pcipcipci", MqttQualityOfService.AtMostOnce);
                client2.MessageStream.Subscribe(msg => returnFormMqtt(msg.Topic, msg.Payload));
                sw.Stop();
                CreateLog("Init Broker client2", $"OK time {sw.ElapsedMilliseconds} ms.");
            }
            catch (Exception ex)
            {
                sw.Stop();
                CreateLog("Init Broker2 Exception", $"{ex.Message} time {sw.ElapsedMilliseconds} [ms]");
            }
            finally
            {
                sw.Stop();
            }
        }

        /// <summary>
        /// Obsługa wiadomości z brokera MQTT
        /// </summary>
        /// <param name="topic">Subskrybowant Topic</param>
        /// <param name="payload">Wiadomość</param>
        private void returnFormMqtt(string topic, byte[] payload)
        {
            Stopwatch = new Stopwatch();
            Stopwatch.Start();
            var t = topic;
            var txt = Encoding.UTF8.GetString(payload == null ? new byte[1] { 0 } : payload);
            string callback = txt;
            int idx = 0;

            // pierwsza opcja
            ArrayList parameters = new ArrayList();
            parameters.Add(address);
            parameters.Add(temperature);
            parameters.Add(fanStatus);

            if (!String.IsNullOrEmpty(callback))
            {
                string temp = String.Empty;

                for (int i = 0; i < parameters.Count; i++)
                {
                    idx = callback.IndexOf('-');
                    temp = callback.Substring(0, idx < 0 ? callback.Length : idx);
                    callback = callback.Remove(0, idx + 1);
                    parameters[i] = temp;
                }


                try
                {
                    using (InzDataBase db = new InzDataBase())
                    {
                        TemperatureTable tt = new TemperatureTable();
                        tt.Temperature = (string)parameters[1];
                        tt.Address = (string)parameters[0];
                        tt.Date = DateTime.Now;
                        tt.FanStatus = byte.Parse(parameters[2].ToString());
                        db.TemperatureTables.Add(tt);
                        db.SaveChanges();
                        Stopwatch.Stop();
                        var ms = Stopwatch.ElapsedMilliseconds;
                        Logs logs = new Logs();
                        logs.Title = $"Instert id={tt.Id} OK";
                        logs.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        logs.RefTempId = tt.Id;
                        logs.Description = $"Operation time = {ms} [ms]";
                        db.Logs1.Add(logs);
                        db.SaveChanges();
                    }

                }
                catch (Exception ex)
                {
                    eventLog1.WriteEntry("[SERVICE-ECEPTION]" + ex.Message);
                    using (InzDataBase db = new InzDataBase())
                    {
                        Stopwatch.Stop();
                        var ms = Stopwatch.ElapsedMilliseconds;
                        Logs logs = new Logs();
                        logs.Title = "Instert ERROR";
                        logs.Description = $"Message:{ex.Message}\t" +
                                            $"Operation time = {ms} [ms]";
                        db.Logs1.Add(logs);
                        db.SaveChanges();
                    }
                }
            }
        }

        /// <summary>
        /// Metoda wykonująca się przy uruchamianiu usługi
        /// </summary>
        protected override void OnStart(string[] args)
        {

            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            eventLog1.WriteEntry("In OnStart.");
            // Set up a timer that triggers every minute.
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 60000; // 60 seconds            
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            System.Threading.Thread thd = new System.Threading.Thread(this.DeleteOldRecords);
            thd.Start();
        }

        /// <summary>
        /// Usuwanie rekordów starsze niż 7 dni od aktualnej daty
        /// </summary>
        private void DeleteOldRecords()
        {
            using (InzDataBase db = new InzDataBase())
            {

                var sevenDaysAgo = DateTime.Now.AddDays(-7);
                if (db.TemperatureTables.Any(x => x.Date < sevenDaysAgo))
                {
                    var query = db.TemperatureTables.Where(x => x.Date < sevenDaysAgo).ToList();
                    foreach (var item in query)
                    {
                        var temp = db.TemperatureTables.Where(x => x.Id == item.Id).FirstOrDefault();
                        if (temp != null)
                        {
                            db.TemperatureTables.Remove(temp);
                            int rows = -1;
                            try
                            {
                                rows = db.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                CreateLog("DeleteError", ex.Message.ToString());
                            }

                            if (rows > 0)
                                CreateLog("Delete successed !", $"for Id= {item.Id}");
                        }
                    }
                }
            }
           
        }

        /// <summary>
        /// Metoda wykonująca się przy zatrzymaniu usługi
        /// </summary>
        protected override void OnStop()
        {
            eventLog1.WriteEntry("In OnStop.");
        }

        /// <summary>
        /// Wywołana co jedną minutę, jej wpis do rejetru EventView informuje o poprawnym działniu metody
        /// </summary>
        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            eventLog1.WriteEntry("Monitoring the System", EventLogEntryType.Information, eventId++);

            if (DateTime.Now > StartDate)
            {
                System.Threading.Thread thd = new System.Threading.Thread(this.DeleteOldRecords);
                thd.Start();
            }
        }

        /// <summary>
        /// Tworzy wpis do tabeli z logami
        /// </summary>
        /// <param name="title"></param>
        /// <param name="desc"></param>
        public static void CreateLog(string title, string desc)
        {
            using (InzDataBase db = new InzDataBase())
            {
                Logs logs = new Logs();
                logs.Title = title;
                logs.Description = $"Message: {desc}\t";
                logs.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                // logs.RefTempId = "";
                db.Logs1.Add(logs);
                db.SaveChanges();
            }
        }
    }
}
=======
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Net.Mqtt;
using System.Collections;

namespace InzService
{
    public partial class InzService : ServiceBase
    {
        private EventLog eventLog1;
        private int eventId = 1;

        private string mqttBroker1 = "mqtt.eclipse.org";
        private string mqttBroker2 = "test.mosquitto.org";
        private string clientId = Guid.NewGuid().ToString();

        private string temperature;
        private string address;
        SessionState ss1, ss2;
        IMqttClient client, client2;
        DateTime StartDate = DateTime.Now;


        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        Stopwatch Stopwatch;

        public InzService()
        {
            InitializeComponent();
            eventLog1 = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("InzServiceSource"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "InzServiceSource", "InzServiceLog");
            }
            eventLog1.Source = "InzServiceSource";
            eventLog1.Log = "InzServiceLog";


            // => MQTT
            this.InitBroker();
            // <MQTT
        }
        private async void InitBroker()
        {
            MqttConfiguration config = new MqttConfiguration();

            Stopwatch sw = new Stopwatch();


            try
            {
                sw.Start();
                client = await MqttClient.CreateAsync(mqttBroker1, config);
                //client2 = await MqttClient.CreateAsync(mqttBroker2, config2);


                ss1 = await client.ConnectAsync(new MqttClientCredentials(clientId: this.clientId.Replace("-", String.Empty)));
                //ss2 = await client.ConnectAsync(new MqttClientCredentials(clientId: this.clientId.Replace("-", String.Empty)));

                await client.SubscribeAsync("pcipcipci", MqttQualityOfService.AtMostOnce);
                //await client2.SubscribeAsync("pcipcipci", MqttQualityOfService.AtMostOnce);


                client.MessageStream.Subscribe(msg => returnFormMqtt(msg.Topic, msg.Payload));
                //client2.MessageStream.Subscribe(msg => returnFormMqtt(msg.Topic, msg.Payload));
                sw.Stop();
                CreateLog("Init Broker client1", $"OK time {sw.ElapsedMilliseconds} ms.");

            }
            catch (Exception ex)
            {
                sw.Stop();
                CreateLog("Init Broker1 Exception", $"{ex.Message} time {sw.ElapsedMilliseconds} [ms]");
            }

            try
            {
                sw.Start();
                // CreateLog("after sw.start", "");
                MqttConfiguration config2 = new MqttConfiguration();
                // CreateLog("after config2", "");
                //client = await MqttClient.CreateAsync(mqttBroker1, config);
                client2 = await MqttClient.CreateAsync(mqttBroker2, config2);
                //CreateLog("after client2", "");

                //ss1 = await client.ConnectAsync(new MqttClientCredentials(clientId: this.clientId.Replace("-", String.Empty)));
                ss2 = await client2.ConnectAsync(new MqttClientCredentials(clientId: this.clientId.Replace("-", String.Empty)));
                //CreateLog("after ss2", "");
                //await client.SubscribeAsync("pcipcipci", MqttQualityOfService.AtMostOnce);
                await client2.SubscribeAsync("pcipcipci", MqttQualityOfService.AtMostOnce);
                //CreateLog("after lient2.SubscribeAsync", "");

                //client.MessageStream.Subscribe(msg => returnFormMqtt(msg.Topic, msg.Payload));
                client2.MessageStream.Subscribe(msg => returnFormMqtt(msg.Topic, msg.Payload));
                //CreateLog("after  client2.MessageStream", "");
                sw.Stop();
                //CreateLog("after sw.Stop", "");
                CreateLog("Init Broker client2", $"OK time {sw.ElapsedMilliseconds} ms.");
            }
            catch (Exception ex)
            {
                sw.Stop();
                CreateLog("Init Broker2 Exception", $"{ex.Message} time {sw.ElapsedMilliseconds} [ms]");
            }
            finally
            {
                sw.Stop();
            }
        }

        private void returnFormMqtt(string topic, byte[] payload)
        {
            Stopwatch = new Stopwatch();
            Stopwatch.Start();
            var t = topic;
            var txt = Encoding.UTF8.GetString(payload == null ? new byte[1] { 0 } : payload);
            //var txt1 = Encoding.UTF32.GetString(payload);
            //var txt2 = Encoding.UTF7.GetString(payload);

            string callback = txt;
            int idx = 0;

            // pierwsza opcja
            ArrayList parameters = new ArrayList();
            parameters.Add(address);
            parameters.Add(temperature);

            if (!String.IsNullOrEmpty(callback))
            {
                string temp = String.Empty;

                for (int i = 0; i < parameters.Count; i++)
                {
                    idx = callback.IndexOf('-');
                    temp = callback.Substring(0, idx < 0 ? callback.Length : idx);
                    callback = callback.Remove(0, idx + 1);
                    parameters[i] = temp;
                }


                try
                {
                    using (InzDataBase db = new InzDataBase())
                    {
                        TemperatureTable tt = new TemperatureTable();
                        tt.Temperature = (string)parameters[1];
                        tt.Address = (string)parameters[0];
                        tt.Date = DateTime.Now;
                        db.TemperatureTables.Add(tt);
                        db.SaveChanges();

                        Stopwatch.Stop();
                        var ms = Stopwatch.ElapsedMilliseconds;
                        Logs logs = new Logs();
                        logs.Title = $"Instert id={tt.Id} OK";
                        logs.Date = DateTime.Now.ToString();
                        logs.RefTempId = tt.Id;
                        logs.Description = $"Operation time = {ms} [ms]";
                        db.Logs1.Add(logs);
                        db.SaveChanges();
                    }

                }
                catch (Exception ex)
                {
                    eventLog1.WriteEntry("[SERVICE-ECEPTION]" + ex.Message);
                    using (InzDataBase db = new InzDataBase())
                    {
                        Stopwatch.Stop();
                        var ms = Stopwatch.ElapsedMilliseconds;
                        Logs logs = new Logs();
                        logs.Title = "Instert ERROR";
                        logs.Description = $"Message:{ex.Message}\t" +
                                            $"Operation time = {ms} [ms]";
                        db.Logs1.Add(logs);
                        db.SaveChanges();
                    }
                }
            }
        }

        protected override void OnStart(string[] args)
        {
           
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus(); 
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus); 

            eventLog1.WriteEntry("In OnStart.");
            // Set up a timer that triggers every minute.
            Timer timer = new Timer();
            timer.Interval = 60000; // 60 seconds            
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start(); 

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING; 
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            this.DeleteOldRecords();
        }

        private void DeleteOldRecords()
        {
            using (InzDataBase db = new InzDataBase())
            {
                var sevedDaysAgo = DateTime.Now.AddDays(-7);
                if (db.TemperatureTables.Any(x => x.Date < sevedDaysAgo))
                {
                    var query = db.TemperatureTables.Where(x => x.Date < sevedDaysAgo).ToList();

                    foreach (var item in query)
                    {

                        db.Logs1.Remove(db.Logs1.Where(x => x.RefTempId == item.Id).FirstOrDefault());
                        CreateLog("Delete successed !", $"for Id= {item.Id}");
                        db.SaveChanges();
                    }

                    var lists = db.TemperatureTables.Where(x => x.Date < sevedDaysAgo).ToList();
                    db.TemperatureTables.RemoveRange(lists);
                    db.SaveChanges();

                }
            }
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("In OnStop.");
        }

        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            eventLog1.WriteEntry("Monitoring the System", EventLogEntryType.Information, eventId++);

            if (DateTime.Now > StartDate)
            {
                DeleteOldRecords();
            }
        }

        public static void CreateLog(string title, string desc)
        {
            using (InzDataBase db = new InzDataBase())
            {
                Logs logs = new Logs();
                logs.Title = title;
                logs.Description = $"Message: {desc}\t";
                // logs.RefTempId = "";
                db.Logs1.Add(logs);
                db.SaveChanges();
            }
        }
    }
}
>>>>>>> 7634a5a11bd73e313484bf67d9f5b1588400d1aa
