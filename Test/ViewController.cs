using System;
using System.Collections.Generic;
using CoreBluetooth;
using FloBLEPlus.iOS;
using Foundation;
using UIKit;

namespace Test
{
public partial class ViewController : UIViewController, ICBCentralManagerDelegate
	{
		private List<CBPeripheral> peripherals;
		private ABTBluetoothReaderManager manager;
		public ABTBluetoothReader Reader;
		private CBCentralManager centralManager;
		public ReaderDelegate ReaderDelegate;
		public ReaderManagerDelegate ManagerDelegate;

		protected ViewController(IntPtr handle) : base(handle)
		{
			// Note: this .ctor should not contain any initialization logic.
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			this.manager = new ABTBluetoothReaderManager();
			this.ManagerDelegate = new ReaderManagerDelegate(this);
			this.manager.WeakDelegate = this.ManagerDelegate;
			this.centralManager = new CBCentralManager(this, null);
			this.centralManager.WeakDelegate = this;
			this.peripherals = new List<CBPeripheral>();
			// Perform any additional setup after loading the view, typically from a nib.
		}

		public override void DidReceiveMemoryWarning()
		{
			base.DidReceiveMemoryWarning();
			// Release any cached data, images, etc that aren't in use.
		}

		public virtual void UpdatedState(CBCentralManager central)
		{
			if (central.State == CBCentralManagerState.PoweredOn)
			{
				centralManager.ScanForPeripherals(peripheralUuids: null, options: (NSDictionary)null);
			}
			else
			{
				centralManager.StopScan();
			}
		}

		[Export("centralManager:didDiscoverPeripheral:advertisementData:RSSI:")]
		public void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber RSSI)
		{
			if (!peripherals.Contains(peripheral) && !string.IsNullOrWhiteSpace(peripheral.Name) && peripheral.Name.StartsWith("ACR", StringComparison.Ordinal))
			{
				centralManager.ConnectPeripheral(peripheral);
				peripherals.Add(peripheral);
			}
		}

		[Export("centralManager:didConnectPeripheral:")]
		public virtual void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral) // TODO: Changed!
		{
			manager.DetectReaderWithPeripheral(peripheral);
		}
	}
	public class ReaderManagerDelegate : ABTBluetoothReaderManagerDelegate
	{
		private ViewController _helper;
		public ReaderManagerDelegate(ViewController helper)
		{
			_helper = helper;
		}

		public override void DidDetectReader(ABTBluetoothReaderManager bluetoothReaderManager, ABTBluetoothReader reader, CBPeripheral peripheral, NSError error)
		{
			if (error == null)
			{
				_helper.Reader?.Detach();

				_helper.Reader = reader;
				_helper.ReaderDelegate = new ReaderDelegate(_helper);
				_helper.Reader.WeakDelegate = _helper.ReaderDelegate;
				_helper.Reader.AttachPeripheral(peripheral);
			}
			else
			{
				// Disconnect, not a reader:
				//_helper.centralManager.CancelPeripheralConnection(peripheral); // TODO: Maybe needed?
			}
		}
	}

	public class ReaderDelegate : ABTBluetoothReaderDelegate
	{
		private ViewController _helper;

		public ReaderDelegate(ViewController helper)
		{
			_helper = helper;
		}
		public override void DidAttachPeripheral(ABTBluetoothReader bluetoothReader, CBPeripheral peripheral, NSError error)
		{
			if (error == null)
			{
				_helper.Reader.AuthenticateWithMasterKey(@"41 43 52 31 32 35 35 55 2D 4A 31 20 41 75 74 68".ToByteArray());
			}
		}

		public override void DidChangeCardStatus(ABTBluetoothReader bluetoothReader, ABTBluetoothReaderCardStatus cardStatus, NSError error)
		{

			if (error == null)
			{
				// Power on card to get ATR:
				switch (cardStatus)
				{
					case ABTBluetoothReaderCardStatus.Present:
						bluetoothReader.PowerOnCard();
						break;

					case ABTBluetoothReaderCardStatus.Absent:
						// Tag lost
						break;
				}
			}
		}

		public override void DidReturnEscapeResponse(ABTBluetoothReader bluetoothReader, NSData response, NSError error)
		{
			base.DidReturnEscapeResponse(bluetoothReader, response, error);
		}

		public override void DidReturnResponseApdu(ABTBluetoothReader bluetoothReader, NSData apdu, NSError error)
		{
			base.DidReturnResponseApdu(bluetoothReader, apdu, error);
		}

		public override void DidReturnAtr(ABTBluetoothReader bluetoothReader, NSData atr, NSError error)
		{
			base.DidReturnAtr(bluetoothReader, atr, error);
		}

		public override void DidAuthenticateWithError(ABTBluetoothReader bluetoothReader, NSError error)
		{
			base.DidAuthenticateWithError(bluetoothReader, error);
		}
	}
}
