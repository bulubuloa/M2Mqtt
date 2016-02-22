using Android.App;
using Android.Widget;
using Android.OS;
using uPLibrary.Networking.M2Mqtt;
using System;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MqttXamarinAndroid
{
	[Activity (Label = "MqttXamarinAndroid", MainLauncher = true, Icon = "@mipmap/icon")]
	public class MainActivity : Activity
	{
		private EditText edtBroker, edtPort, edtUser, edtPass, edtTopic, edtMessage;
		private Button bttConnect, bttSub, bttPub;
		private CheckBox cbxUser;
		private Spinner spnQOS;
		private TextView txtResult;

		private MqttClient mqttClient;

		private void initWidget()
		{
			this.txtResult = FindViewById<TextView> (Resource.Id.result);
			this.edtBroker = FindViewById<EditText> (Resource.Id.edtBroker);
			this.edtPort = FindViewById<EditText> (Resource.Id.edtPort);
			this.edtUser = FindViewById<EditText> (Resource.Id.edtUser);
			this.edtPass = FindViewById<EditText> (Resource.Id.edtPassword);
			this.edtTopic = FindViewById<EditText> (Resource.Id.edtTopic);
			this.edtMessage = FindViewById<EditText> (Resource.Id.edtMessage);
			this.bttConnect = FindViewById<Button> (Resource.Id.bttConnect);
			this.bttSub = FindViewById<Button> (Resource.Id.bttSubTopic);
			this.bttPub = FindViewById<Button> (Resource.Id.bttPublishMes);
			this.cbxUser = FindViewById<CheckBox> (Resource.Id.cbxUser);
			this.spnQOS = FindViewById<Spinner> (Resource.Id.spnQOS);

			var adapter = ArrayAdapter.CreateFromResource (this, Resource.Array.qos, Android.Resource.Layout.SimpleSpinnerItem);
			adapter.SetDropDownViewResource (Android.Resource.Layout.SimpleSpinnerDropDownItem);
			spnQOS.Adapter = adapter;

			this.cbxUser.CheckedChange += (object sender, CompoundButton.CheckedChangeEventArgs e) => {
				if(e.IsChecked){
					edtUser.Enabled = true;
					edtPass.Enabled = true;
				}else{
					edtUser.Enabled = false;
					edtPass.Enabled = false;
				}
			};
		}

		private void initControl()
		{
			bttConnect.Click += (object sender, System.EventArgs e) => {
				ConnectServer();
			};

			bttSub.Click += (object sender, EventArgs e) => {
				try {
					if(edtTopic.Text != null || edtTopic.Text.Length > 0){
						if(mqttClient!=null && mqttClient.IsConnected){
							mqttClient.Subscribe(new string[] { edtTopic.Text }, new byte[] { (byte)spnQOS.SelectedItemPosition });
							txtResult.Text = "Subcribe topic "+edtTopic.Text+" ok";
						}
					}else{
						Toast.MakeText (this, "topic wrong", ToastLength.Short).Show();
					}
				} catch (Exception ex) {
					
				}
			};
		}

		private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
		{
			string result = System.Text.Encoding.UTF8.GetString(e.Message);

			RunOnUiThread (() => {
				txtResult.Text = "Receiver message: "+result;
			});
		}

		private void client_ConnectionClosedEvent (object sender, EventArgs e)
		{
			txtResult.Text = "Connection lost";
		}

		private void ConnectServer()
		{
			try {
				if(edtBroker.Text == null  || edtBroker.Text.Length == 0 || edtPort.Text == null || edtPort.Text.Length == 0){
					Toast.MakeText (this, "Broken or port wrong", ToastLength.Short).Show();
				}else{
					mqttClient = new MqttClient(edtBroker.Text);
					mqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
					mqttClient.ConnectionClosed += client_ConnectionClosedEvent;
					if(cbxUser.Checked){
						mqttClient.Connect ("HOANGDEPTRAI", edtUser.Text, edtPass.Text);
					}else{
						mqttClient.Connect ("HOANGDEPTRAI");
					}
					//mqttClient.Connect (customerDB.customerId+"", Mqtt_Username, Mqtt_Password, false, KeepAlives);
					if(mqttClient.IsConnected){
						txtResult.Text = "Connect OK -- let's sub topic";
					}
				}
			} catch (Exception ex) {
				txtResult.Text = "Connect ERROR";
			}
		}

		protected override void OnCreate (Bundle savedInstanceState)
		{
			Xamarin.Insights.Initialize (XamarinInsights.ApiKey, this);
			base.OnCreate (savedInstanceState);
			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			initWidget ();
			initControl ();
		}
	}
}
