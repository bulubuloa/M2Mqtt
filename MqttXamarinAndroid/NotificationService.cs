
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

using Android.Net;
using Android.Support.V7.Widget;

namespace MqttXamarinAndroid
{
	[Service ( Name="vn.proship.proshipforcustomer.NotificationService")]			
	public class NotificationService : Service
	{
		static object locker = new object ();
		private const string TAG = "NotificationService";
		private const ushort KeepAlives = 65535;
		private const int Mqtt_Port = 1883;
		private const string Mqtt_Broker = "tracking.proship.vn";
		private const string Mqtt_Username = "chuxuanhy";
		private const string Mqtt_Password = "0936160721";
		private const string Mqtt_Id = "QUACHHOANGDEPTRAI";
		private string Mqtt_Topic = "MqttExample";
		private bool isConnected = false, isConnecing = false, isStop = false;

		private MqttClient mqttClient;
		private NetWorkEvent networkEvent;
		private IntentFilter networkEventFilter;

		private void DebugError(string message)
		{
			Android.Util.Log.Error (TAG,message);
		}

		public void createNotification(String title, String body) {
			Random rnd = new Random();
			int month = rnd.Next(1, 99999); 

			Notification.Builder builder = new Notification.Builder (this)
				.SetContentTitle (title)
				.SetContentText (body)
				.SetSmallIcon (Resource.Drawable.monoandroidsplash)
				.SetDefaults (NotificationDefaults.Sound);

			Notification.BigTextStyle textStyle = new Notification.BigTextStyle();
			textStyle.BigText (body);
			builder.SetStyle (textStyle);	

			Notification notification = builder.Build();
			NotificationManager notificationManager = GetSystemService (Context.NotificationService) as NotificationManager;

			notificationManager.Notify (month, notification);
		}

		private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
		{
			string result = System.Text.Encoding.UTF8.GetString(e.Message);
			//ProcessMessage (e.Topic,result);
		}

		private void client_ConnectionClosedEvent (object sender, EventArgs e)
		{
			if (!isStop) {
				if (isOnline ()) {
					if (!isConnecing) {
						isConnected = false;
						if (mqttClient != null) {
							try {
								mqttClient = null;
								ReconnectIfNecessary ();
							} catch (Exception ex) {
								mqttClient = null;
								ReconnectIfNecessary ();
								DebugError (ex.StackTrace);
							}
						} else {
							ReconnectIfNecessary ();
						}
					}
				} else {
					mqttClient = null;
					isConnecing = false;
					isConnected = false;
				}
			}
		}

		private void client_NetworkStatusEvent(object sender, EventArgs e)
		{
			PowerManager pm = (PowerManager)GetSystemService(Context.PowerService);
			PowerManager.WakeLock wl = pm.NewWakeLock(WakeLockFlags.Partial, "MQTT");
			wl.Acquire();
			if (isOnline ()) {
				if(!isConnecing) {
					if(!isConnected) {
						mqttClient=null;
						ReconnectIfNecessary();
					}
				}
			}
			wl.Release();
		}

		private void ConnectServer()
		{
			try {
				isConnecing = true;
				isConnected = false;
				mqttClient = new MqttClient(Mqtt_Broker);
				mqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
				mqttClient.ConnectionClosed += client_ConnectionClosedEvent;
				mqttClient.Connect(Mqtt_Id,Mqtt_Username,Mqtt_Password,false, 0, false, null, null, false, KeepAlives);
				if(mqttClient.IsConnected){
					isConnecing = false;
					isConnected = true;
					mqttClient.Subscribe(new string[] { Mqtt_Topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
					DebugError("Connect and Sub Done!");
				}
			} catch (Exception ex) {
				DebugError (ex.StackTrace);
			}
		}

		private void ReconnectIfNecessary()
		{
			try {
				if(mqttClient==null){
					ConnectServer();
				}
			} catch (Exception ex) {
				DebugError (ex.StackTrace);
			}
		}

		private void SendKeepAlives()
		{
			lock (locker) {
				try {
					if(!isConnected) {
						mqttClient=null;
						ReconnectIfNecessary();
					} else {
						mqttClient.Publish (Mqtt_Topic, new byte[]{0}, (byte)MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
					}
				} catch (Exception ex) {
					DebugError (ex.StackTrace);
				}
			}
		}
			
		private void RegisterInternetBroadcast()
		{
			networkEventFilter = new IntentFilter ();
			networkEventFilter.AddAction (Android.Net.ConnectivityManager.ConnectivityAction);
			networkEvent = new NetWorkEvent ();
			networkEvent.ConnectionStatusChanged += client_NetworkStatusEvent;
			RegisterReceiver (networkEvent,networkEventFilter);
		}

		private void UnregisterInternetBroadcast()
		{
			if (networkEvent != null) {
				UnregisterReceiver (networkEvent);
			}
		}

		public override IBinder OnBind (Intent intent)
		{
			throw new NotImplementedException ();
		}

		public override void OnTaskRemoved (Intent rootIntent)
		{
			if (!isStop) {
				try{
					Intent restartServiceIntent = new Intent(ApplicationContext, this.Class);
					restartServiceIntent.SetPackage(this.PackageName);
					PendingIntent restartServicePendingIntent = PendingIntent.GetService (ApplicationContext, 1, restartServiceIntent, PendingIntentFlags.OneShot);
					AlarmManager alarmService = (AlarmManager) Application.GetSystemService(Context.AlarmService);
					alarmService.Set(AlarmType.ElapsedRealtime, SystemClock.ElapsedRealtime() + 1000, restartServicePendingIntent);
				}catch (Exception e){
					DebugError (e.StackTrace);
				}
			}
			base.OnTaskRemoved (rootIntent);
		}

		public override void OnCreate ()
		{
			base.OnCreate ();
		}

		public override void OnDestroy ()
		{
			isStop = true;
			try {
				if (mqttClient != null && mqttClient.IsConnected) {
					mqttClient.Disconnect ();
				}
			} catch (Exception ex) {
				DebugError (ex.StackTrace);
			}
			mqttClient = null;
			UnregisterInternetBroadcast ();
			base.OnDestroy ();
		}

		public override StartCommandResult OnStartCommand (Intent intent, StartCommandFlags flags, int startId)
		{
			ConnectServer ();
			RegisterInternetBroadcast ();
			return StartCommandResult.Sticky;
		}

		public bool isOnline() {
			ConnectivityManager connectivityManager = (ConnectivityManager)GetSystemService(ConnectivityService);
			NetworkInfo activeConnection = connectivityManager.ActiveNetworkInfo;
			return (activeConnection != null) && activeConnection.IsConnected;
		}

		[BroadcastReceiver(Enabled = true)]
		[IntentFilter(new[] { Android.Net.ConnectivityManager.ConnectivityAction })]
		public class NetWorkEvent : BroadcastReceiver
		{
			public event EventHandler ConnectionStatusChanged;
			public override void OnReceive(Context context, Intent intent)
			{
				if (ConnectionStatusChanged != null)
					ConnectionStatusChanged(this, EventArgs.Empty);
			}
		}
	}
}