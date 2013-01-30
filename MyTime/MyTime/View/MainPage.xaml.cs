﻿// ***********************************************************************
// Assembly         : MyTime
// Author           : trevo_000
// Created          : 11-03-2012
//
// Last Modified By : trevo_000
// Last Modified On : 11-11-2012
// ***********************************************************************
// <copyright file="MainPage.xaml.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml.Serialization;
using FieldService.SocietyScraper;
using FieldService.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using MyTimeDatabaseLib;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;
using Microsoft.Phone.Marketplace;
using TimeData = MyTimeDatabaseLib.TimeData;

namespace FieldService
{
	/// <summary>
	/// Class MainPage
	/// </summary>
	public partial class MainPage : PhoneApplicationPage
	{
		/// <summary>
		/// The _DT
		/// </summary>
		private DispatcherTimer _dt;
		/// <summary>
		/// The _timer
		/// </summary>
		private TimeSpan _timer;
		/// <summary>
		/// The _timer base
		/// </summary>
		private DateTime _timerBase;

		/// <summary>
		/// The _timer state
		/// </summary>
		private TimerState _timerState = TimerState.Stopped;

		private BackgroundWorker _bwLoadRvs;


		// Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="MainPage" /> class.
		/// </summary>
		public MainPage()
		{
			//CheckForCrashFile();

			InitializeComponent();

			// Set the data context of the listbox control to the sample data
			DataContext = App.ViewModel;
			Loaded += MainPage_Loaded;


			_bwLoadRvs = new BackgroundWorker();

			_bwLoadRvs.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);

			//selector.DataSource = new IntLoopingDataSource() {MinValue = 0, MaxValue = 100, SelectedItem = 0};
		}


		/// <summary>
		/// Gets or sets the start time.
		/// </summary>
		/// <value>The start time.</value>
		[XmlElement(ElementName = "startTime", DataType = "DateTime")]
		public DateTime StartTime { get; set; }


		/// <summary>
		/// Gets or sets the icon.
		/// </summary>
		/// <value>The icon.</value>
		[EditorBrowsable]
		public Object Icon { get; set; }

		/// <summary>
		/// Gets the timer time span.
		/// </summary>
		/// <value>The timer time span.</value>
		private TimeSpan TimerTimeSpan { get { return _timerState == TimerState.Stopped ? new TimeSpan() : DateTime.Now.Subtract(_timerBase); } }

		#region Events

		/// <summary>
		/// Handles the SelectionChanged event of the ListBox control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
		private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (((ListBox) sender).SelectedIndex < 0) return;
			try {
				var rv = (ReturnVisitItemViewModel) ((ListBox) sender).SelectedItem;
				((ListBox) sender).SelectedIndex = -1;
				NavigationService.Navigate(new Uri(string.Format("/View/AddNewRV.xaml?id={0}", rv.ItemId.ToString(CultureInfo.InvariantCulture)), UriKind.Relative));
			} catch {}
		}

		// Load data for the ViewModel lbRvItems
		/// <summary>
		/// Handles the Loaded event of the MainPage control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
		private void MainPage_Loaded(object sender, RoutedEventArgs e)
		{
			bool countCalls = bool.Parse(App.AppSettingsProvider["AddCallPlacements"].Value);
			if (countCalls) {
				tbReturnVisits.Visibility = Visibility.Collapsed;
			}
			if (!App.ViewModel.IsMainMenuLoaded) App.ViewModel.LoadMainMenu();

			_bwLoadRvs.RunWorkerAsync();
		}

		private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			App.ViewModel.LoadReturnVisitList(SortOrder.DateOldestToNewest);
			
			_timerState = TimerState.Stopped;
			if (IsolatedStorageFile.GetUserStoreForApplication().FileExists("restart.bin")) GetRestartTime();

			lblDailyTextDate.Text = DateTime.Now.ToLongDateString();
			if (lblDailyTextDate.Text.Length >= 26) {
				lblDailyTextDate.Text = string.Format("{0:ddd} {0:MMM}. {0:dd}, {0:yyyy}", DateTime.Today);
			}

			var ss = new DailyTextScraper();

			ss.DailyTextRetrieved += ss_DailyTextRetrieved;

			ss.StartDailyTextRetrieval(DateTime.Now);
		}

		/// <summary>
		/// Handles the TapEvent event of the MenuImage control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="GestureEventArgs" /> instance containing the event data.</param>
		public void MenuImage_TapEvent(object sender, GestureEventArgs e)
		{
			var r = sender as System.Windows.Shapes.Rectangle;
			if (r == null) return;
			var v = r.Tag.ToString().ToLower();
			NavigateMainMenu(v);
		}

		/// <summary>
		/// Handles the ClickEvent event of the MenuItem control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
		public void MenuItem_ClickEvent(object sender, RoutedEventArgs e)
		{
			var mi = sender as MenuItem;
			if (mi == null) return;
			string v = mi.Header.ToString().ToLower();
			NavigateMainMenu(v);
		}

		/// <summary>
		/// Handles the KeyDown event of the TextBoxMasking control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="KeyEventArgs" /> instance containing the event data.</param>
		private void TextBoxMasking_KeyDown(object sender, KeyEventArgs e)
		{
			Key[] goodKeys = {
								 Key.D0, Key.D1, Key.D2, Key.D3, Key.D4,
								 Key.D5, Key.D6, Key.D7, Key.D8, Key.D9,
								 Key.NumPad0, Key.NumPad1, Key.NumPad2, Key.NumPad3, Key.NumPad4,
								 Key.NumPad5, Key.NumPad6, Key.NumPad7, Key.NumPad8, Key.NumPad9
							 };
			if (!goodKeys.Contains(e.Key)) {
				e.Handled = true;
			}
		}

		/// <summary>
		/// Handles the Click event of the abibAddIt control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
		private void abibAddIt_Click(object sender, EventArgs e) {
			TimeAdditClickTapEvent();
		}

		private void TimeAdditClickTapEvent()
		{
			if(_timerState == TimerState.Running) PauseTimer();
			TimeSpan t = TimerTimeSpan;

			if (t.TotalMinutes <= 0) {
				App.ToastMe("You can't add zero minutes.");
				return;
			}

			try {
				if (App.AppSettingsProvider["roundtime"].Value.Equals(bool.TrueString, StringComparison.CurrentCultureIgnoreCase)) {
					var ts = TimeSpan.FromMinutes(15 * Math.Ceiling(t.TotalMinutes / 15.0));
					float m = float.Parse(t.TotalMinutes.ToString()) % (float)15.0;
					if (m <= 7.5) t = TimeSpan.FromMinutes(ts.TotalMinutes - 15.0);
					else t = ts;
				}
			} catch { }


			var minutes = (int) t.TotalMinutes;

			if (minutes <= 0) {
				App.ToastMe("You can't add zero minutes.");
				return;
			}

			var td = new TimeData {
									  Date = DateTime.Now,
									  Minutes = minutes,
									  Magazines = (int)tbMags.Value,
									  Brochures = (int) tbBrochures.Value,
									  Books = (int) tbBooks.Value,
									  BibleStudies = (int) tbBibleStudies.Value,
									  ReturnVisits = (int) tbReturnVisits.Value,
									  Notes = tbNotes.Text
								  };

			try {
				TimeDataInterface.AddTime(ref td);
				App.ToastMe(string.Format("Time ({0} hrs & {1} min) added.", t.Hours, t.Minutes));
				TimerStopClickTapEvent();
				ResetText();
			} catch (Exception ee) {
				//TODO:Exception handler
				MessageBox.Show("Couldn't add time.\n\nException: " + ee.Message);
			}
		}

		/// <summary>
		/// Handles the Click event of the abibPause control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
		private void abibPause_Click(object sender, EventArgs e) {
			TimerPauseClickTapEvent();
		}

		private void TimerPauseClickTapEvent()
		{
			object sender;
			EventArgs e;
			switch (_timerState) {
				case TimerState.Paused:
				case TimerState.Stopped:
					TimerStartClickTapEvent();
					break;
				default: // TimerState.Running
					PauseTimer();
					break;
			}
		}

		/// <summary>
		/// Handles the Click event of the abibStart control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
		private void abibStart_Click(object sender, EventArgs e) {
			TimerStartClickTapEvent();
		}

		private void TimerStartClickTapEvent()
		{
			switch (_timerState) {
				case TimerState.Running:
					break;
				case TimerState.Paused:
					_timerBase = DateTime.Now.Subtract(_timer);
					_dt.Start();
					break;
				default: // TimerState.Stopped
					_timerBase = DateTime.Now;
					_dt = new DispatcherTimer {
												  Interval = new TimeSpan(0, 0, 0, 1, 0)
											  };
					_dt.Tick += dt_Tick;
					_dt.Start();
					_timer = new TimeSpan(0, 0, 1);
					break;
			}
			_timerState = TimerState.Running;
			SetRestartTime();
			lblTimer.Text = string.Format("{0:0,0}:{1:0,0}:{2:0,0}", _timer.Hours, _timer.Minutes, _timer.Seconds);
		}

		/// <summary>
		/// Handles the Click event of the abibStop control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
		private void abibStop_Click(object sender, EventArgs e) { TimerStopClickTapEvent(); }

		/// <summary>
		/// Handles the Tick event of the dt control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
		private void dt_Tick(object sender, EventArgs e)
		{
			if (_timerState != TimerState.Running) {
				_dt.Stop();
				return;
			}
			_timer = TimerTimeSpan;

			lblTimer.Text = string.Format("{0:0,0}:{1:0,0}:{2:0,0}", _timer.Hours, _timer.Minutes, _timer.Seconds);
		}

		/// <summary>
		/// Handles the Tap event of the imgShowAllReturnVisit control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="GestureEventArgs" /> instance containing the event data.</param>
		private void imgShowAllReturnVisit_Tap(object sender, GestureEventArgs e) { NavigationService.Navigate(new Uri("/View/ReturnVisitFullList.xaml", UriKind.Relative)); }

		/// <summary>
		/// SS_s the daily text retrieved.
		/// </summary>
		/// <param name="dt">The dt.</param>
		public void ss_DailyTextRetrieved(DailyText dt)
		{
			lblDailyText.Text = dt.Scripture;
			lblDTSummary.Text = dt.SummaryText;
		}

		#endregion

		/// <summary>
		/// Pauses the timer.
		/// </summary>
		private void PauseTimer()
		{
			try {
				_dt.Stop();
				SetRestartTime();
			} catch { } finally {
				_timerState = TimerState.Paused;
			}
		}

		/// <summary>
		/// Stops the timer.
		/// </summary>
		private void TimerStopClickTapEvent()
		{
			try {
				_dt.Stop();
				_timer = new TimeSpan(0,0,0);
				_timerBase = new DateTime();
				lblTimer.Text = "00:00:00";
			} catch { } finally {
				tbMags.Value = 0;
				tbBrochures.Value = 0;
				tbBooks.Value = 0;
				tbBibleStudies.Value = 0;
				tbReturnVisits.Value = 0;
				tbNotes.Text = string.Empty;
				_timerState = TimerState.Stopped;
				ClearRestartTime();
			}

		}

		/// <summary>
		/// Gets the restart time.
		/// </summary>
		private void GetRestartTime()
		{
			try {
				using (IsolatedStorageFileStream restartTime = IsolatedStorageFile.GetUserStoreForApplication().OpenFile("restart.bin", FileMode.Open)) {
					var isRunning = (TimerState) restartTime.ReadByte();

					_dt = new DispatcherTimer {
												  Interval = new TimeSpan(0, 0, 0, 1, 0)
											  };
					_dt.Tick += dt_Tick;
					if (isRunning == TimerState.Running) {
						var time = new byte[sizeof (long)];
						restartTime.Read(time, 0, sizeof (long));
						_timerBase = new DateTime(BitConverter.ToInt64(time, 0));
						_timer = DateTime.Now.Subtract(_timerBase);
						_timerState = TimerState.Running;
						_dt.Start();
					} else if (isRunning == TimerState.Paused) {
						var ints = new byte[sizeof (int)];
						restartTime.Read(ints, 0, sizeof (int));
						int hours = BitConverter.ToInt32(ints, 0);
						restartTime.Read(ints, 0, sizeof (int));
						int minutes = BitConverter.ToInt32(ints, 0);
						restartTime.Read(ints, 0, sizeof (int));
						int seconds = BitConverter.ToInt32(ints, 0);
						_timer = new TimeSpan(hours, minutes, seconds);
						_timerBase = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour - _timer.Hours, DateTime.Now.Minute - _timer.Minutes, DateTime.Now.Second - _timer.Seconds);
						_timerState = TimerState.Paused;
					} else {
						_timer = new TimeSpan(0, 0, 0);
					}
					lblTimer.Text = string.Format("{0:0,0}:{1:0,0}:{2:0,0}", _timer.Hours, _timer.Minutes, _timer.Seconds);
				}
			} catch {
				IsolatedStorageFile.GetUserStoreForApplication().DeleteFile("restart.bin");
			}
		}

		/// <summary>
		/// Sets the restart time.
		/// </summary>
		private void SetRestartTime()
		{
			IsolatedStorageFile.GetUserStoreForApplication().DeleteFile("restart.bin");
			try {
				using (IsolatedStorageFileStream restart = IsolatedStorageFile.GetUserStoreForApplication().CreateFile("restart.bin")) {
					if (_timerState == TimerState.Running) {
						restart.WriteByte((byte) TimerState.Running);
						byte[] time = BitConverter.GetBytes(_timerBase.Ticks);
						restart.Write(time, 0, sizeof (long));
					} else if (_timerState == TimerState.Paused) {
						restart.WriteByte((byte) TimerState.Paused);
						byte[] hours = BitConverter.GetBytes(_timer.Hours);
						byte[] minutes = BitConverter.GetBytes(_timer.Minutes);
						byte[] seconds = BitConverter.GetBytes(_timer.Seconds);
						restart.Write(hours, 0, sizeof (int));
						restart.Write(minutes, 0, sizeof (int));
						restart.Write(seconds, 0, sizeof (int));
					} else {
						restart.WriteByte((byte) TimerState.Stopped);
					}
				}
			} catch {
				IsolatedStorageFile.GetUserStoreForApplication().DeleteFile("restart.bin");
			}
		}

		/// <summary>
		/// Clears the restart time.
		/// </summary>
		private void ClearRestartTime() { IsolatedStorageFile.GetUserStoreForApplication().DeleteFile("restart.bin"); }

		/// <summary>
		/// Navigates the main menu.
		/// </summary>
		/// <param name="v">The v.</param>
		private void NavigateMainMenu(string v)
		{
			string month = DateTime.Today.ToString("MMMM").ToLower() + " report";
			switch (v) {
				case "add time":
					NavigationService.Navigate(new Uri("/View/RegularTime.xaml", UriKind.Relative));
					break;
				case "add rbc time":
					NavigationService.Navigate(new Uri("/View/AddRBCTime.xaml", UriKind.Relative));
					break;
				case "add return visit":
					NavigationService.Navigate(new Uri("/View/AddNewRV.xaml", UriKind.Relative));
					break;
				case "watchtower library":
					var wbTask = new WebBrowserTask {Uri = new Uri("http://wol.jw.org", UriKind.RelativeOrAbsolute)};
					wbTask.Show();
					break;
				case "service year report":
					ShowYearlyReport();
					break;
				case "send service report":
					SendServiceReport();
					break;
				case "settings":
					NavigationService.Navigate(new Uri("/View/SettingsPage.xaml", UriKind.Relative));
					break;
				case "buy cloud backup":
					var mdt = new MarketplaceDetailTask();
					mdt.Show();
					break;
				case "backup & restore":
					NavigationService.Navigate(new Uri("/View/BackupAndRestorePage.xaml", UriKind.Relative));
					break;
				default:
					if (v.Equals(month)) {
						ShowMonthlyReport();
					}
					break;
			}
		}

		/// <summary>
		/// Sends the service report.
		/// </summary>
		private void SendServiceReport()
		{
			var from = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
			DateTime tod = new DateTime(from.Year, from.Month, 1).AddMonths(1).AddDays(-1);

			string body = string.Format("Here is my field service report for {0:MMMM}, {0:yyyy}:\n\n", from);
			TimeData[] entries = Reporting.BuildTimeReport(from, tod, SortOrder.DateOldestToNewest);

			int tTime = 0;
			int tMags = 0;
			int tBks = 0;
			int tBros = 0;
			int tRv = 0;
			int tBs = 0;
			foreach (TimeData e in entries) {
				tTime += e.Minutes;
				tMags += e.Magazines;
				tBks += e.Books;
				tBros += e.Brochures;
				tRv += e.ReturnVisits;
				tBs += e.BibleStudies;
			}

			body += string.Format("Hours:\t\t{0:0.00}\n", ((double) tTime/60.0));
			var x = RBCTimeDataInterface.GetMonthRBCTimeTotal(from);
			body += x > 0 ? string.Format("R/B/C Hours: \t{0:0.0}\n", ((double) x/60.0)) : string.Empty;
			body += tMags > 0 ? string.Format("Magazines:\t{0}\n", tMags) : string.Empty;
			body += tBks  > 0 ? string.Format("Books:\t\t{0}\n", tBks) : string.Empty;
			body += tBros > 0 ? string.Format("Brochures:\t{0}\n",tBros) : string.Empty;
			body += tRv > 0 ? string.Format("Return Visits:\t{0}\n",tRv) : string.Empty;
			body += tBs > 0 ? string.Format("Bible Studies:\t{0}",tBs) : string.Empty;


			Reporting.SendReport(body);
		}

		/// <summary>
		/// Shows the yearly report.
		/// </summary>
		private void ShowYearlyReport()
		{
			DateTime from = DateTime.Today.Month >= 9 ? new DateTime(DateTime.Today.Year, 9, 1) : new DateTime(DateTime.Today.Year - 1, 9, 1);
			DateTime to = from.AddYears(1).AddDays(-1);

			NavigationService.Navigate(new Uri(string.Format("/View/TimeReport.xaml?from={0}&to={1}", from.ToString("MM-dd-yyyy"), to.ToString("MM-dd-yyyy")), UriKind.Relative));
		}

		/// <summary>
		/// Shows the monthly report.
		/// </summary>
		private void ShowMonthlyReport()
		{
			var from = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
			DateTime to = new DateTime(@from.Year, @from.Month, 1).AddMonths(1).AddDays(-1);

			NavigationService.Navigate(new Uri(string.Format("/View/TimeReport.xaml?from={0}&to={1}", from.ToString("MM-dd-yyyy"), to.ToString("MM-dd-yyyy")), UriKind.Relative));
		}

		/// <summary>
		/// Resets the text.
		/// </summary>
		private void ResetText()
		{
			tbBibleStudies.Value = 0;
			tbBooks.Value = 0;
			tbBrochures.Value = 0;
			tbMags.Value = 0;
			tbNotes.Text = string.Empty;
			tbReturnVisits.Value = 0;
		}

		#region Nested type: TimerState

		/// <summary>
		/// Enum TimerState
		/// </summary>
		private enum TimerState : byte
		{
			/// <summary>
			/// The stopped
			/// </summary>
			Stopped = 3,
			/// <summary>
			/// The paused
			/// </summary>
			Paused = 0,
			/// <summary>
			/// The running
			/// </summary>
			Running = 1
		};

		#endregion

		private void abibStart_Tap(object sender, GestureEventArgs e) { TimerStartClickTapEvent(); }

		private void abibPause_Tap(object sender, GestureEventArgs e) { TimerPauseClickTapEvent(); }

		private void abibStop_Tap(object sender, GestureEventArgs e) { TimerStopClickTapEvent(); }

		private void abibAddIt_Tap(object sender, GestureEventArgs e) { TimeAdditClickTapEvent(); }
	}
}