using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace com.rogeriolima.beacon
{
    public enum BroadcastMode 
	{
	send	= 0,
	receive	= 1,
	unknown = 2
}
	public enum BroadcastState 
	{
		inactive = 0,
		active	 = 1
	}


	public class BeaconController : MonoBehaviour
	{
		[SerializeField] private Button _bluetoothButton;
		[SerializeField] private TMP_Text debugText, _bluetoothText, blueToothStatus, closestName, closestDistance, scanBeacons;
		[SerializeField] private List<TMP_Text> uuids = new List<TMP_Text>();
		[SerializeField] private List<TMP_Text> distances = new List<TMP_Text>();
		// Beacon BroadcastState (Start, Stop)
		[SerializeField] private Image img_ButtonBroadcastState;
		[SerializeField] private Image beaconImage;
		[SerializeField] private Sprite ice, mint, blueberry;
		
		[SerializeField] private Color scanning, stopped;
		private BroadcastMode bm_Mode;
		private BroadcastState bs_State;
		private BeaconType bt_Type;
		private BeaconType bt_PendingType;
		private string s_UUID;
		private string s_Major;
		private string s_Minor;
		private string s_Region;
		private int i_BeaconCounter = 0;
		private List<Beacon> mybeacons = new List<Beacon>();

		private Dictionary<string,string> foundBeacon = new Dictionary<string, string>();
		// Start is called before the first frame update
		void Start()
		{
			
			FillBeaconsDictionary();
			setBeaconPropertiesAtStart();
			img_ButtonBroadcastState.color = stopped;
			
			_bluetoothButton.onClick.AddListener(delegate() 
			{
				BluetoothState.EnableBluetooth();
			});

			BluetoothState.BluetoothStateChangedEvent += delegate(BluetoothLowEnergyState state) 
			{
				switch (state) {
				case BluetoothLowEnergyState.TURNING_OFF:
				case BluetoothLowEnergyState.TURNING_ON:
					break;

				case BluetoothLowEnergyState.UNKNOWN:
				case BluetoothLowEnergyState.RESETTING:
					// _statusText.text = "Checking Device…";
					break;

				case BluetoothLowEnergyState.UNAUTHORIZED:
					// _statusText.text = "You don't have the permission to use beacons.";
					break;

				case BluetoothLowEnergyState.UNSUPPORTED:
					// _statusText.text = "Your device doesn't support beacons.";
					break;

				case BluetoothLowEnergyState.POWERED_OFF:
					_bluetoothButton.interactable = true;
					_bluetoothText.text = "Enable Bluetooth";
					break;

				case BluetoothLowEnergyState.POWERED_ON:
					_bluetoothButton.interactable = false;
					_bluetoothText.text = "Bluetooth already enabled";
					break;

				case BluetoothLowEnergyState.IBEACON_ONLY:
					_bluetoothButton.interactable = false;
					_bluetoothText.text = "iBeacon only";
					break;

				default:
					// _statusText.text = "Unknown Error";
					break;
				}
			};

			BluetoothState.Init();
		}

		private void FillBeaconsDictionary()
		{
			foundBeacon.Add("50749", "ice");
			foundBeacon.Add("40736", "mint");
			foundBeacon.Add("50161", "Blueberry");
		}

		private void Update() 
		{
			BluetoothLowEnergyState b_state = BluetoothState.GetBluetoothLEStatus();
			if(b_state == BluetoothLowEnergyState.POWERED_ON)
			{
				_bluetoothButton.gameObject.SetActive(false);
			}
			else
			{
				_bluetoothButton.gameObject.SetActive(true);
			}
			blueToothStatus.text = b_state.ToString();
		}

		private void setBeaconPropertiesAtStart() 
		{
			RestorePlayerPrefs();

			if (bm_Mode == BroadcastMode.unknown) 
			{ // first start
				bm_Mode = BroadcastMode.receive;
				// _statusText.text = "BeaconMode :"+ bm_Mode.ToString();

				bt_Type = BeaconType.iBeacon;

				if (iBeaconServer.region.regionName != "") 
				{
					Debug.Log("check iBeaconServer-inspector");
					debugText.text = "check iBeaconServer-inspector";

					s_Region = iBeaconServer.region.regionName;
					bt_Type 	= iBeaconServer.region.beacon.type;

					if (bt_Type == BeaconType.EddystoneURL) 
					{
						s_UUID = iBeaconServer.region.beacon.UUID;
					} 
					else if (bt_Type == BeaconType.EddystoneUID) 
					{
						s_UUID = iBeaconServer.region.beacon.UUID;
						s_Major = iBeaconServer.region.beacon.instance;
					} 
					else if (bt_Type == BeaconType.iBeacon) 
					{
						s_UUID = iBeaconServer.region.beacon.UUID;
						s_Major = iBeaconServer.region.beacon.major.ToString();
						s_Minor = iBeaconServer.region.beacon.minor.ToString();
					}
				} 
				else if (iBeaconReceiver.regions.Length != 0) 
				{
					Debug.Log("check iBeaconReceiver-inspector");
					debugText.text = "check iBeaconReceiver-inspector";
					s_Region	= iBeaconReceiver.regions[0].regionName;
					debugText.text = iBeaconReceiver.regions[0].regionName;


					bt_Type 	= iBeaconReceiver.regions[0].beacon.type;
					if (bt_Type == BeaconType.EddystoneURL) 
					{
						s_UUID = iBeaconReceiver.regions[0].beacon.UUID;
					} 
					else if (bt_Type == BeaconType.EddystoneUID) 
					{
						s_UUID = iBeaconReceiver.regions[0].beacon.UUID;
						s_Major = iBeaconReceiver.regions[0].beacon.instance;
					} 
					else if (bt_Type == BeaconType.iBeacon) 
					{
						s_UUID = iBeaconReceiver.regions[0].beacon.UUID;
						s_Major = iBeaconReceiver.regions[0].beacon.major.ToString();
						s_Minor = iBeaconReceiver.regions[0].beacon.minor.ToString();
					} 
				}
				
			}

			if (iBeaconReceiver.regions.Length == 0)
			{
				debugText.text = "Zero iBeaconReceiver regions available";
			}

			bs_State = BroadcastState.inactive;
			Debug.Log("Beacon properties and modes restored");
			debugText.text = "Beacon properties and modes restored";
		}

		
		public void btn_StartStop() 
		{
			//Debug.Log("Button Start / Stop pressed");
			/*** Beacon will start ***/
			if (bs_State == BroadcastState.inactive) 
			{
				// ReceiveMode
				if (bm_Mode == BroadcastMode.receive) 
				{
					iBeaconReceiver.BeaconRangeChangedEvent += OnBeaconRangeChanged;

					// check if all mandatory propertis are filled
					if (s_Region == null || s_Region == "") 
					{
						debugText.text = "Null region";
						return;
					}
					if (bt_Type == BeaconType.Any) 
					{
						iBeaconReceiver.regions = new iBeaconRegion[]{new iBeaconRegion(s_Region, new Beacon())};
					} 
					else if (bt_Type == BeaconType.EddystoneEID) 
					{
						iBeaconReceiver.regions = new iBeaconRegion[]{new iBeaconRegion(s_Region, new Beacon(BeaconType.EddystoneEID))};
					} 
					else 
					{
						if (s_UUID == null || s_UUID == "") 
						{
							debugText.text = "Null UUID";
							return;
						}
						if (bt_Type == BeaconType.iBeacon) 
						{
							iBeaconReceiver.regions = new iBeaconRegion[]{new iBeaconRegion(s_Region, new Beacon(s_UUID, Convert.ToInt32(s_Major), Convert.ToInt32(s_Minor)))};
							debugText.text = "iBeacon in range";
						} 
						else if (bt_Type == BeaconType.EddystoneUID) 
						{
							iBeaconReceiver.regions = new iBeaconRegion[]{new iBeaconRegion(s_Region, new Beacon(s_UUID, "")) };
							debugText.text = "Eddystone in range";
						} 
						else if (bt_Type == BeaconType.EddystoneURL) 
						{
							iBeaconReceiver.regions = new iBeaconRegion[]{new iBeaconRegion(s_Region, new Beacon(s_UUID))};
							debugText.text = "EddystoneURL in range";
						}
					}
					
					debugText.text = "Listening for beacons";
					// !!! Bluetooth has to be turned on !!! TODO
					iBeaconReceiver.Scan();
					Debug.Log ("Listening for beacons");
				}
				// SendMode
				else 
				{
					// check if all mandatory propertis are filled
					if (s_Region == null || s_Region == "") 
					{
						return;
					}


					if (bt_Type == BeaconType.Any) 
					{
						iBeaconServer.region = new iBeaconRegion(s_Region, new Beacon());
					} 
					else 
					{
						if (s_UUID == null || s_UUID == "") 
						{

							return;
						}

						if (bt_Type == BeaconType.EddystoneURL) 
						{
							iBeaconServer.region = new iBeaconRegion(s_Region, new Beacon(s_UUID));
						} 
						else 
						{
							if (s_Major == null || s_Major == "") 
							{
								return;
							}

							if (bt_Type == BeaconType.EddystoneUID) 
							{
								iBeaconServer.region = new iBeaconRegion(s_Region, new Beacon(s_UUID, s_Major));
							} 
							else if (bt_Type == BeaconType.iBeacon) 
							{
								if (s_Minor == null || s_Minor == "") 
								{
									return;
								}

								iBeaconServer.region = new iBeaconRegion(s_Region, new Beacon(s_UUID, Convert.ToInt32(s_Major), Convert.ToInt32(s_Minor)));
							}
						}
					}
					// !!! Bluetooth has to be turned on !!! TODO
					iBeaconServer.Transmit();
					Debug.Log ("It is on, go sending");
					debugText.text = "It is on, go sending";
				}

				bs_State = BroadcastState.active;
				img_ButtonBroadcastState.color = scanning;
				scanBeacons.text = "Searching";

			} else 
			{
				if (bm_Mode == BroadcastMode.receive) {// Stop for receive
					iBeaconReceiver.Stop();
					iBeaconReceiver.BeaconRangeChangedEvent -= OnBeaconRangeChanged;
					debugText.text = "Receiver stopped";
				} 
				else 
				{ // Stop for send
					iBeaconServer.StopTransmit();
				}
				bs_State = BroadcastState.inactive;
				img_ButtonBroadcastState.color = stopped;
				scanBeacons.text = "Search";
			}

			SavePlayerPrefs();
		}


		private void OnBeaconRangeChanged(Beacon[] beacons) 
		{ 

			foreach (Beacon b in beacons) 
			{
				var index = mybeacons.IndexOf(b);
				if (index == -1) 
				{
					mybeacons.Add(b);
				} 
				else 
				{
					mybeacons[index] = b;
				}
			}

			for (int i = mybeacons.Count - 1; i >= 0; --i) 
			{
				if (mybeacons[i].lastSeen.AddSeconds(10) < DateTime.Now) 
				{
					mybeacons.RemoveAt(i);
				}
			}

			SetupUIInfos();
		}

		private void SetupUIInfos()
		{
			Dictionary<float, string> distancesList = new Dictionary<float, string>();
			List<float> allDistances = new List<float>();
			for(int i = 0; i < mybeacons.Count; i++)
			{
				if(i<= uuids.Count-1 && mybeacons[i].type == BeaconType.iBeacon)
				{
					// float distanceToBeacon = (float) mybeacons[i].accuracy;
					float distanceToBeacon = (float)mybeacons[i].accuracy;
					distanceToBeacon = Truncate(distanceToBeacon,2);
					uuids[i].text = "Beacon "+i+": "+ foundBeacon[mybeacons[i].major.ToString()];
					distances[i].text = "Distance: "+ distanceToBeacon+ " m";
					distancesList.Add(distanceToBeacon,foundBeacon[mybeacons[i].major.ToString()]);
					allDistances.Add(distanceToBeacon);
				}
			}

			allDistances.Sort();
			closestDistance.text = "Distance: "+allDistances[0].ToString()+" m";
			string closestBeaconName =  distancesList[allDistances[0]];
			closestName.text = "You are in region: "+closestBeaconName;

			switch(closestBeaconName)
			{
				case "ice":
					beaconImage.sprite = ice;
					break;
				case "mint":
					beaconImage.sprite = mint;
					break;
				case "Blueberry":
					beaconImage.sprite = blueberry;
					break;
			}

			if(mybeacons.Count == 0)
			{
				debugText.text = "Não adianta fazer bico, não tem beacons por perto";
			}
		}
		

		#region PlayerPrefs
		private void SavePlayerPrefs() {
			PlayerPrefs.SetInt("Type", (int)bt_Type);
			PlayerPrefs.SetString("Region", s_Region);
			PlayerPrefs.SetString("UUID", s_UUID);
			PlayerPrefs.SetString("Major", s_Major);
			PlayerPrefs.SetString("Minor", s_Minor);
			PlayerPrefs.SetInt("BroadcastMode", (int)bm_Mode);
			//PlayerPrefs.DeleteAll();
		}
		private void RestorePlayerPrefs() {
			if (PlayerPrefs.HasKey("Type"))
				bt_Type = (BeaconType)PlayerPrefs.GetInt("Type");
			if (PlayerPrefs.HasKey("Region"))
				s_Region = PlayerPrefs.GetString("Region");
			if (PlayerPrefs.HasKey("UUID"))
				s_UUID = PlayerPrefs.GetString("UUID");
			if (PlayerPrefs.HasKey("Major"))
				s_Major = PlayerPrefs.GetString("Major");
			if (PlayerPrefs.HasKey("Minor"))
				s_Minor = PlayerPrefs.GetString("Minor");
			if (PlayerPrefs.HasKey("BroadcastMode"))
				bm_Mode = (BroadcastMode)PlayerPrefs.GetInt("BroadcastMode");
			else 
				bm_Mode = BroadcastMode.unknown;
		}
		#endregion 
		private float Truncate(float value, int digits)
		{
			double mult = Math.Pow(10.0, digits);
			double result = Math.Truncate( mult * value ) / mult;
			return (float) result;
		}
		
	}
}




