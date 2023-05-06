// SendKeysGUI
// Graphical front end for sendkeys, sends clipboard, text, a file or command output to sendkeys.exe
// Markus Scholtes, 2020+2023
//
// WPF "all in one file" program, no Visual Studio or MSBuild is needed to compile
// Version for .Net 4.x

/* compile with:
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe /target:winexe SendKeysGUI.cs /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\presentationframework.dll" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\windowsbase.dll" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\presentationcore.dll" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\System.Xaml.dll" /win32icon:MScholtes.ico
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Xml;

// set attributes
using System.Reflection;
[assembly:AssemblyTitle("Graphical front end for sendkeys")]
[assembly:AssemblyDescription("Graphical front end for sendkeys")]
[assembly:AssemblyConfiguration("")]
[assembly:AssemblyCompany("MS")]
[assembly:AssemblyProduct("SendKeysGUI")]
[assembly:AssemblyCopyright("© Markus Scholtes 2023")]
[assembly:AssemblyTrademark("")]
[assembly:AssemblyCulture("")]
[assembly:AssemblyVersion("2.0.0.0")]
[assembly:AssemblyFileVersion("2.0.0.0")]

namespace WPFApplication
{
	public class CustomWindow : Window
	{
		static string editorToUse = "notepad.exe";

		// create window object out of XAML string
		public static CustomWindow LoadWindowFromXaml(string xamlString)
		{ // Get the XAML content from a string.
			// prepare XML document
			XmlDocument XAML = new XmlDocument();
			// read XAML string
			XAML.LoadXml(xamlString);
			// and convert to XML
			XmlNodeReader XMLReader = new XmlNodeReader(XAML);
			// generate WPF object tree
			CustomWindow objWindow = (CustomWindow)XamlReader.Load(XMLReader);

			string iniPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
			((TextBox)objWindow.FindName("FileToSend")).Text = iniPath + "\\FileToSend.txt";
			if (!Directory.Exists(iniPath))
			{
				Directory.CreateDirectory(iniPath);

				string directoryOfExecutable = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
				if (directoryOfExecutable.StartsWith("file:\\") || directoryOfExecutable.StartsWith("http:\\")) { directoryOfExecutable = directoryOfExecutable.Substring(6); }

				if (File.Exists(Path.Combine(directoryOfExecutable, "Config.ini")))
				{ // copy initial config file if present
					try { File.Copy(Path.Combine(directoryOfExecutable, "Config.ini"), Path.Combine(iniPath, "Config.ini")); } catch { }
				}

				if (File.Exists(Path.Combine(directoryOfExecutable, "FileToSend.txt")))
				{ // copy initial file to send if present
					try { File.Copy(Path.Combine(directoryOfExecutable, "FileToSend.txt"), Path.Combine(iniPath, "FileToSend.txt")); } catch { }
				}
			}
			if (Directory.Exists(iniPath))
			{
				IniFile iniFile = new IniFile(iniPath + "\\Config.ini");
				if (!File.Exists(iniPath + "\\Config.ini"))
				{
					for (int i = 0; i <= 9; i++) iniFile.Write("Settings", "Text" + i.ToString(), "");
					for (int i = 0; i <= 9; i++) iniFile.Write("Settings", "TextShift" + i.ToString(), "");
					iniFile.Write("Settings", "TextShiftSZ", "");
					iniFile.Write("Settings", "TextShiftAccent", "");
					iniFile.Write("Settings", "Editor", editorToUse);
					iniFile.Write("Settings", "FileToSend", "");
					iniFile.Write("Settings", "Command", "for /l %i in (1,1,10) do @echo %i.");
					iniFile.Write("Settings", "Modifier", "Ctrl");
					iniFile.Write("Settings", "Wait", "1500");
				}
				for (int i = 0; i <= 9; i++)
				{
					((TextBox)objWindow.FindName("TextToSend" + i.ToString())).Text = iniFile.Read("Settings", "Text" + i.ToString());
					((TextBox)objWindow.FindName("TextShiftToSend" + i.ToString())).Text = iniFile.Read("Settings", "TextShift" + i.ToString());
				}
				((TextBox)objWindow.FindName("TextShiftToSendSZ")).Text = iniFile.Read("Settings", "TextShiftSZ");
				((TextBox)objWindow.FindName("TextShiftToSendAccent")).Text = iniFile.Read("Settings", "TextShiftAccent");
				if (iniFile.Read("Settings", "Editor") != "") editorToUse = iniFile.Read("Settings", "Editor");
				if (iniFile.Read("Settings", "FileToSend") != "") ((TextBox)objWindow.FindName("FileToSend")).Text = iniFile.Read("Settings", "FileToSend");
				((TextBox)objWindow.FindName("CommandToSend")).Text = iniFile.Read("Settings", "Command");
				if (iniFile.Read("Settings", "Wait") != "") ((TextBox)objWindow.FindName("Wait")).Text = iniFile.Read("Settings", "Wait");
				if (iniFile.Read("Settings", "Modifier").ToUpper() == "ALT")
				{
					((CheckBox)objWindow.FindName("HotkeyModifier")).IsChecked = true;
					objWindow.useAltModifier = true;
				}
			}

			// return CustomWindow object
			return objWindow;
		}

		// helper function that "climbs up" the parent object chain from a window object until the root window object is reached
		private FrameworkElement FindParentWindow(object sender)
		{
			FrameworkElement GUIControl = (FrameworkElement)sender;
			while ((GUIControl.Parent != null) && (GUIControl.GetType() != typeof(CustomWindow)))
			{
				GUIControl = (FrameworkElement)GUIControl.Parent;
			}

			if (GUIControl.GetType() == typeof(CustomWindow))
				return GUIControl;
			else
				return null;
		}

		// event handlers

		// left mouse click on button "Clipboard"
		private void ClipboardButton_Click(object sender, RoutedEventArgs e)
		{
			// event is handled afterwards
			e.Handled = true;

			if (!Clipboard.ContainsText())
			{ // check if there is text content in clipboard, if not return
				MessageBox.Show("No text in clipboard", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			// send keys of text in clipboard
			SendKeys(sender, Clipboard.GetText().Replace("\r\n", "{ENTER}").Replace("\n", "{ENTER}"));
		}

		// left mouse click on button "Text"
		private void TextButton_Click(object sender, RoutedEventArgs e)
		{
			// event is handled afterwards
			e.Handled = true;

			// retrieve window parent object
			Window objWindow = (Window)FindParentWindow(sender);
			// if not found then end
			if (objWindow == null) { return; }

			string strTextToSend = "TextToSend" + (((Button)sender).Name).Substring(8);
			if ((((Button)sender).Name).Contains("SendTextShift")) strTextToSend = "TextShiftToSend" + (((Button)sender).Name).Substring(13);

			// read content of TextBox control
			TextBox objTextToSend = (TextBox)objWindow.FindName(strTextToSend);
			if (objTextToSend.Text == "")
			{
				MessageBox.Show("Text to send is missing", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// send keys of text in text box
			SendKeys(sender, objTextToSend.Text);
		}

		// left mouse click on button "Send file"
		private void SendFileButton_Click(object sender, RoutedEventArgs e)
		{
			// event is handled afterwards
			e.Handled = true;

			// retrieve window parent object
			Window objWindow = (Window)FindParentWindow(sender);
			// if not found then end
			if (objWindow == null) { return; }

			// read content of TextBox control
			TextBox objFileToSend = (TextBox)objWindow.FindName("FileToSend");
			if (objFileToSend.Text == "")
			{ // no file name supplied
				MessageBox.Show("File name missing", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (!File.Exists(objFileToSend.Text))
			{ // file does not exist or access error
				MessageBox.Show("Cannot open file '" + objFileToSend.Text + "'", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			try
			{
				string fileContent;
				// open the text file using a stream reader
				using (StreamReader sr = new StreamReader(objFileToSend.Text))
				{
					// read the stream to a string
					fileContent = sr.ReadToEnd();
				}

				if (fileContent.Length > 0)
				{	// send keys of text in file
					SendKeys(sender, fileContent.Replace("\r\n", "{ENTER}").Replace("\n", "{ENTER}"));
				}
				else
				{ // file is empty
					MessageBox.Show("File '" + objFileToSend.Text + "' is empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			catch (Exception ex)
			{ // error reading file
				MessageBox.Show("Cannot read file '" + objFileToSend.Text + "'.\r\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		// left mouse click on button "Rescan config"
		private void RescanConfigButton_Click(object sender, RoutedEventArgs e)
		{
			// event is handled afterwards
			e.Handled = true;

			// retrieve window parent object
			Window objWindow = (Window)FindParentWindow(sender);
			// if not found then end
			if (objWindow == null) { return; }

			string iniFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + "\\Config.ini";

			if (File.Exists(iniFilePath))
			{
				IniFile iniFile = new IniFile(iniFilePath);
				for (int i = 0; i <= 9; i++)
				{
					((TextBox)objWindow.FindName("TextToSend" + i.ToString())).Text = iniFile.Read("Settings", "Text" + i.ToString());
					((TextBox)objWindow.FindName("TextShiftToSend" + i.ToString())).Text = iniFile.Read("Settings", "TextShift" + i.ToString());
				}
				((TextBox)objWindow.FindName("TextShiftToSendSZ")).Text = iniFile.Read("Settings", "TextShiftSZ");
				((TextBox)objWindow.FindName("TextShiftToSendAccent")).Text = iniFile.Read("Settings", "TextShiftAccent");
				if (iniFile.Read("Settings", "Editor") != "") editorToUse = iniFile.Read("Settings", "Editor");
				if (iniFile.Read("Settings", "FileToSend") != "") ((TextBox)objWindow.FindName("FileToSend")).Text = iniFile.Read("Settings", "FileToSend");
				((TextBox)objWindow.FindName("CommandToSend")).Text = iniFile.Read("Settings", "Command");
				if (iniFile.Read("Settings", "Wait") != "") ((TextBox)objWindow.FindName("Wait")).Text = iniFile.Read("Settings", "Wait");
				if (iniFile.Read("Settings", "Modifier").ToUpper() == "ALT")
				{
					if (((CheckBox)objWindow.FindName("HotkeyModifier")).IsChecked == false) ((CheckBox)objWindow.FindName("HotkeyModifier")).IsChecked = true;
				}
				else
				{
					if (((CheckBox)objWindow.FindName("HotkeyModifier")).IsChecked == true) ((CheckBox)objWindow.FindName("HotkeyModifier")).IsChecked = false;
				}
			}
		}

		// left mouse click on button "Edit file" or "Edit config"
		private void EditFileButton_Click(object sender, RoutedEventArgs e)
		{
			// event is handled afterwards
			e.Handled = true;

			// retrieve window parent object
			Window objWindow = (Window)FindParentWindow(sender);
			// if not found then end
			if (objWindow == null) { return; }

			string editFile;
			if (((Button)sender).Name == "EditFile")
			{ // edit file to send
				// read content of TextBox control
				TextBox objFileToEdit = (TextBox)objWindow.FindName("FileToSend");
				if (objFileToEdit.Text == "")
				{ // no file name supplied
					MessageBox.Show("File name missing", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
				editFile = objFileToEdit.Text;
			}
			else
			{ // edit configuration file of SendKeysGUI
				editFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + "\\Config.ini";
			}

			// start editor with file
			StartProcess(editorToUse, editFile, false, false, false);
		}

		// left mouse click on button "Command"
		private void CommandButton_Click(object sender, RoutedEventArgs e)
		{
			// event is handled afterwards
			e.Handled = true;

			// retrieve window parent object
			Window objWindow = (Window)FindParentWindow(sender);
			// if not found then end
			if (objWindow == null) { return; }

			// read content of TextBox control
			TextBox objCommandToSend = (TextBox)objWindow.FindName("CommandToSend");
			if (objCommandToSend.Text == "")
			{ // no command supplied
				MessageBox.Show("Command missing", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// start command and collect output (and error)
			string commandOutput = StartProcess("cmd.exe", "/c " + objCommandToSend.Text, false, false, true);
			// send keys of output
			if (commandOutput != "") SendKeys(sender, commandOutput.Replace("\r\n", "{ENTER}").Replace("\n", "{ENTER}"));
		}

		// left mouse click on button "Sendkeys help"
		private void HelpButton_Click(object sender, RoutedEventArgs e)
		{
			// event is handled afterwards
			e.Handled = true;

			// starting command to display help text in console window
			StartProcess("cmd.exe", "/c sendkeys.exe & pause", false, false, false);
		}

		// left mouse click on checkbox "HotkeyModifier"
		private void HotkeyModifier_Click(object sender, RoutedEventArgs e)
		{
			// event is handled afterwards
			e.Handled = true;

			// switch between Ctrl and Alt as hotkey modifier, but only if hotkes are registered
			if (bHotkeyRegistered) SwitchHotKeyModifier();
		}

		// mouse moves into button area
		private void Button_MouseEnter(object sender, MouseEventArgs e)
		{
			// retrieve window parent object
			Window objWindow = (Window)FindParentWindow(sender);
			// if found change mouse form
			if (objWindow != null) { objWindow.Cursor = System.Windows.Input.Cursors.Hand; }
		}

		// mouse moves out of button area
		private void Button_MouseLeave(object sender, MouseEventArgs e)
		{
			// retrieve window parent object
			Window objWindow = (Window)FindParentWindow(sender);
			// if found change mouse form
			if (objWindow != null) { objWindow.Cursor = System.Windows.Input.Cursors.Arrow; }
		}

		// click on file picker button ("...")
		private void FilePicker_Click(object sender, RoutedEventArgs e)
		{
			// retrieve window parent object
			Window objWindow = (Window)FindParentWindow(sender);

			// if not found then end
			if (objWindow == null) { return; }

			// create OpenFileDialog control
			Microsoft.Win32.OpenFileDialog objFileDialog = new Microsoft.Win32.OpenFileDialog();

			// set file extension filters for file dialog
			objFileDialog.DefaultExt = ".txt";
			objFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
			objFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

			// display file picker dialog
			Nullable<bool> result = objFileDialog.ShowDialog();

			// file selected?
			if (result.HasValue && result.Value)
			{ // fill Texbox with file name
				TextBox objFileToSend = (TextBox)objWindow.FindName("FileToSend");
				objFileToSend.Text = objFileDialog.FileName;
			}
		}

		// "empty" drag handler
		private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
		{
			e.Effects = DragDropEffects.All;
			e.Handled = true;
		}

		// drop handler: insert filename to textbox
		private void TextBox_PreviewDrop(object sender, DragEventArgs e)
		{
			object objText = e.Data.GetData(DataFormats.FileDrop);
			TextBox objTextBox = sender as TextBox;
			if ((objTextBox != null) && (objText != null))
			{
				objTextBox.Text = string.Format("{0}",((string[])objText)[0]);
			}
		}


		#region global hotkeys
		[DllImport("user32.dll")]
		private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

		[DllImport("user32.dll")]
		private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		//Modifiers:
		private const uint MOD_NONE = 0x0000; //(none)
		private const uint MOD_ALT = 0x0001; //ALT
		private const uint MOD_CONTROL = 0x0002; //CTRL
		private const uint MOD_SHIFT = 0x0004; //SHIFT
		private const uint MOD_WIN = 0x0008; //WINDOWS
		private const uint MOD_NOREPEAT = 0x4000; // do not autorepeat hotkey

		private IntPtr windowHandle;
		private HwndSource sourceHandle;

		// register hotkeys on initialisation
		protected override void OnSourceInitialized(EventArgs e)
		{ // add global hotkey
			base.OnSourceInitialized(e);

			// add window procedure to main window
			windowHandle = new WindowInteropHelper(this).Handle;
			sourceHandle = HwndSource.FromHwnd(windowHandle);
			sourceHandle.AddHook(HwndHook);

			// add hotkeys
			if (useAltModifier)
			{ // use Alt as modifier for hotkey
				// add hotkeys
				RegisterHotkeys(MOD_ALT);
				
				// change tooltip descriptions
				SwitchToolTips("Ctrl-", "Alt-");
			}
			else
			{ // use Ctrl as modifier for hotkey
				// add hotkeys
				RegisterHotkeys(MOD_CONTROL);

			}
			bHotkeyRegistered = true;
		}

		// remove hotkeys on close
		protected override void OnClosed(EventArgs e)
		{ // remove global hotkey

			// remove hotkeys
			for (int i = 0x30; i <= 0x39; i++)
			{
				UnregisterHotKey(windowHandle, i);
				UnregisterHotKey(windowHandle, i+0x100);
			}
			UnregisterHotKey(windowHandle, 0xDB);
			UnregisterHotKey(windowHandle, 0x1DB);
			UnregisterHotKey(windowHandle, 0xDC);
			UnregisterHotKey(windowHandle, 0x1DC);
			UnregisterHotKey(windowHandle, 0xDD);
			UnregisterHotKey(windowHandle, 0x1DD);

			// remove window procedure from main window
			sourceHandle.RemoveHook(HwndHook);

			base.OnClosed(e);
		}

		// Alt or Ctrl as hotkey modifier key?
		bool useAltModifier = false;
		bool bHotkeyRegistered = false;


		// register hotkeys with Modifier MOD_CONTROL or MOD_ALT
		private void RegisterHotkeys(uint uiModifier)
		{ // add hotkeys
			// be cautious: the combination of handle and id must be unique

			for (uint i = 0x30; i <= 0x39; i++)
			{
				RegisterHotKey(windowHandle, (int)i, uiModifier | MOD_NOREPEAT, i); // uiModifier + 0 to uiModifier + 9
				RegisterHotKey(windowHandle, (int)i+0x100, MOD_SHIFT | uiModifier | MOD_NOREPEAT, i); // Shift + uiModifier + 0 to Shift + uiModifier + 9
			}
			RegisterHotKey(windowHandle, 0xDB, uiModifier | MOD_NOREPEAT, 0xDB); // uiModifier + ß
			RegisterHotKey(windowHandle, 0x1DB, MOD_SHIFT | uiModifier | MOD_NOREPEAT, 0xDB); // Shift + uiModifier + ß
			RegisterHotKey(windowHandle, 0xDC, uiModifier | MOD_NOREPEAT, 0xDC); // uiModifier + ^
			RegisterHotKey(windowHandle, 0x1DC, MOD_SHIFT | uiModifier | MOD_NOREPEAT, 0xDC); // Shift + uiModifier + ^
			RegisterHotKey(windowHandle, 0xDD, uiModifier | MOD_NOREPEAT, 0xDD); // uiModifier + ´
			RegisterHotKey(windowHandle, 0x1DD, MOD_SHIFT | uiModifier | MOD_NOREPEAT, 0xDD); // Shift + uiModifier + ´
		}

		// switch between hotkey modifiers
		private void SwitchHotKeyModifier()
		{
			// remove hotkeys
			for (int i = 0x30; i <= 0x39; i++)
			{
				UnregisterHotKey(windowHandle, i);
				UnregisterHotKey(windowHandle, i+0x100);
			}
			UnregisterHotKey(windowHandle, 0xDB);
			UnregisterHotKey(windowHandle, 0x1DB);
			UnregisterHotKey(windowHandle, 0xDC);
			UnregisterHotKey(windowHandle, 0x1DC);
			UnregisterHotKey(windowHandle, 0xDD);
			UnregisterHotKey(windowHandle, 0x1DD);

			// switch modifier flag
			useAltModifier = !useAltModifier;
			if (useAltModifier)
			{ // use Alt as modifier for hotkey
				// add hotkeys
				RegisterHotkeys(MOD_ALT);
				
				// change tooltip descriptions
				SwitchToolTips("Ctrl-", "Alt-");
			}
			else
			{ // use Ctrl as modifier for hotkey
				// add hotkeys
				RegisterHotkeys(MOD_CONTROL);

				// change tooltip descriptions
				SwitchToolTips("Alt-", "Ctrl-");
			}
		}

		private void SwitchToolTips(string oldFragment, string newFragment)
		{ // change tooltip descriptions, replace text fragments

			Button objButton = (Button)this.FindName("Clipboard");
			objButton.ToolTip = ((string)objButton.ToolTip).Replace(oldFragment, newFragment);
			for (int i = 0; i <= 9; i++)
			{ // change send text tooltips
				objButton = (Button)this.FindName("SendText" + i.ToString());
				objButton.ToolTip = ((string)objButton.ToolTip).Replace(oldFragment, newFragment);
				objButton = (Button)this.FindName("SendTextShift" + i.ToString());
				objButton.ToolTip = ((string)objButton.ToolTip).Replace(oldFragment, newFragment);
			}
			objButton = (Button)this.FindName("SendFile");
			objButton.ToolTip = ((string)objButton.ToolTip).Replace(oldFragment, newFragment);
			objButton = (Button)this.FindName("EditFile");
			objButton.ToolTip = ((string)objButton.ToolTip).Replace(oldFragment, newFragment);
			objButton = (Button)this.FindName("SendCommand");
			objButton.ToolTip = ((string)objButton.ToolTip).Replace(oldFragment, newFragment);
			objButton = (Button)this.FindName("SendTextShiftSZ");
			objButton.ToolTip = ((string)objButton.ToolTip).Replace(oldFragment, newFragment);
			objButton = (Button)this.FindName("SendTextShiftAccent");
			objButton.ToolTip = ((string)objButton.ToolTip).Replace(oldFragment, newFragment);
		}

		private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{ // window procedure for hotkeys

			if ((msg == 0x0312) && (lParam != null))// 0x0312 = WM_HOTKEY
			{ // hotkey pressed
				if (this.IsActive)
				{ // never send hotkey to myself, this might result in endless loops and app crashes
					handled = true;
					return IntPtr.Zero;
				}

				int vkey = (((int)lParam >> 16) & 0xFFFF);
				bool bWithShift = (wParam.ToInt32() & 0x100) == 0x100;

				// rule we set above: vkey-code has to be the same as hotkey id in the lowest byte
				if (vkey == (wParam.ToInt32() & 0xFF))
				{
					if ((vkey >= 0x30) && (vkey <= 0x39)) // key '0' to '9' pressed
					{ // send content of textbox 0 to 9
						TextBox objTextToSend;
						if (bWithShift)
						{ objTextToSend = (TextBox)this.FindName("TextShiftToSend" + (vkey-0x30).ToString()); }
						else
						{ objTextToSend = (TextBox)this.FindName("TextToSend" + (vkey-0x30).ToString()); }
						if (objTextToSend.Text != "")
						{ // wait for Alt or Ctrl key release
							if (useAltModifier)
							{ while (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) ; }
							else
							{ while (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) ; }
							// send keys of text field
							SendKeys(this, objTextToSend.Text);
						}
						handled = true;
					}

					if (vkey == 0xDB) // key 'ß' pressed
					{ if (bWithShift)
						{ // Hotkey with Shift: send content of textbox
							TextBox objTextToSend = (TextBox)this.FindName("TextShiftToSendSZ");
							if (objTextToSend.Text != "")
							{ // wait for Alt or Ctrl key release
								if (useAltModifier)
								{ while (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) ; }
								else
								{ while (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) ; }
								// send keys of text field
								SendKeys(this, objTextToSend.Text);
							}
						}
						else
						{ // Hotkey without Shift: send file content
							TextBox objFileToSend = (TextBox)this.FindName("FileToSend");
							if ((objFileToSend.Text != "") && (File.Exists(objFileToSend.Text)))
							{ // file exists
								try
								{
									string fileContent;
									// open the text file using a stream reader
									using (StreamReader sr = new StreamReader(objFileToSend.Text))
									{
										// read the stream to a string
										fileContent = sr.ReadToEnd();
									}

									if (fileContent.Length > 0)
									{ // wait for Alt or Ctrl key release
										if (useAltModifier)
										{ while (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) ; }
										else
										{ while (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) ; }
										// send keys of text in file
										SendKeys(this, fileContent.Replace("\r\n", "{ENTER}").Replace("\n", "{ENTER}"));
									}
								}
								catch
								{ // error reading file
									// do nothing here
								}
							}
						}
						handled = true;
					}

					if (vkey == 0xDC) // key '^' pressed
					{ if (bWithShift)
						{ // Hotkey with Shift: send keys of command output
							TextBox objCommandToSend = (TextBox)this.FindName("CommandToSend");
							if (objCommandToSend.Text != "")
							{ // wait for Alt or Ctrl key release
								if (useAltModifier)
								{ while (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) ; }
								else
								{ while (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) ; }
								// start command and collect output (and error)
								string commandOutput = StartProcess("cmd.exe", "/c " + objCommandToSend.Text, false, false, true);
								// send keys of output
								if (commandOutput != "") SendKeys(this, commandOutput.Replace("\r\n", "{ENTER}").Replace("\n", "{ENTER}"));
							}
						}
						else
						{ // Hotkey without Shift: send clipboard content
							// wait for Alt or Ctrl key release
							if (useAltModifier)
							{ while (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) ; }
							else
							{ while (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) ; }
							// send text in clipboard
							SendKeys(this, Clipboard.GetText().Replace("\r\n", "{ENTER}").Replace("\n", "{ENTER}"));
						}
						handled = true;
					}

					if (vkey == 0xDD) // key '´' pressed
					{ if (bWithShift)
						{ // Hotkey with Shift: send text field
							TextBox objTextToSend = (TextBox)this.FindName("TextShiftToSendAccent");
							if (objTextToSend.Text != "")
							{ // wait for Alt or Ctrl key release
								if (useAltModifier)
								{ while (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) ; }
								else
								{ while (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) ; }
								// send keys of text field
								SendKeys(this, objTextToSend.Text);
							}
						}
						else
						{ // Hotkey without Shift: edit file
							TextBox objFileToEdit = (TextBox)this.FindName("FileToSend");
							if (objFileToEdit.Text != "")
							{ // file name supplied
								// start editor with file
								StartProcess(editorToUse, objFileToEdit.Text, false, false, false);
							}
						}
						handled = true;
					}

				}
			}
			return IntPtr.Zero;
		}
		#endregion


		#region helper functions
		// function to send keys out of event handler
		private void SendKeys(object sender, string keysToSend)
		{
			// retrieve window parent object
			Window objWindow = (Window)FindParentWindow(sender);
			// if not found then end
			if (objWindow == null) { return; }

			string waitString = "";
			FrameworkElement GUIControl = (FrameworkElement)sender;
			if ((GUIControl.Parent != null) && (GUIControl.GetType() != typeof(CustomWindow)))
			{ // read content of TextBox control "Wait"
				TextBox objWait = (TextBox)objWindow.FindName("Wait");
				if (objWait.Text != "")
				{
					int tempInt;
					if (Int32.TryParse(objWait.Text, out tempInt))
						if (tempInt >= 0)
							waitString = objWait.Text;
				}

				if (waitString == "")
				{ // error in wait value
					MessageBox.Show("Impossible value for wait", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
			}
			else
			{
				waitString = "0";
			}

			// read state of CheckBox control "Multilines"
			CheckBox objCheckBox = (CheckBox)objWindow.FindName("MultiLines");
			if (objCheckBox.IsChecked.Value)
			{ // every line gets its own sendkeys command

				// replace keyword {ENTER} in all case variations with uppercase {ENTER}, not really necessary
				// (.Escape("{ENTER}") and .Replace("$","$$") here just to make code "recyclable")
				string upperCaseENTER = Regex.Replace(keysToSend, Regex.Escape("{ENTER}"), "{ENTER}".Replace("$","$$"), RegexOptions.IgnoreCase);
				string[] linesToSend = upperCaseENTER.Split(new string[] { "{ENTER}" }, StringSplitOptions.None);
				int lastindex = linesToSend.Length - 1;
				for (int index = 0; index <= lastindex; index++)
				{ // starting command that send keys in "keysToSend" with selected wait
					if (index == 0)
					{ // wait only for the first call
						if (index < lastindex) // last call without closing {ENTER}
							StartProcess("cmd.exe", "/c sendkeys.exe \"{WAIT " + waitString + "}" + linesToSend[index].Replace("\"", "\"\"") + "{ENTER}\"", true, true, false);
						else
							StartProcess("cmd.exe", "/c sendkeys.exe \"{WAIT " + waitString + "}" + linesToSend[index].Replace("\"", "\"\"") + "\"", true, true, false);
					}
					else
					{ // no wait
						if (index < lastindex) // last call without closing {ENTER}
							StartProcess("cmd.exe", "/c sendkeys.exe \"" + linesToSend[index].Replace("\"", "\"\"") + "{ENTER}\"", true, true, false);
						else
							StartProcess("cmd.exe", "/c sendkeys.exe \"" + linesToSend[index].Replace("\"", "\"\"") + "\"", true, true, false);
					}
				}
			}
			else
			{ // all text in one sendkeys command
				// starting command that send keys in "keysToSend" with selected wait
				StartProcess("cmd.exe", "/c sendkeys.exe \"{WAIT " + waitString + "}" + keysToSend.Replace("\"", "\"\"") + "\"", false, true, false);
			}
		}

		// function to start process
		private string StartProcess(string processName, string parameter, bool waitForExit, bool hideWindow, bool getOutput)
		{
			string output = "";

			ProcessStartInfo startInfo = new ProcessStartInfo
			{ // configuration for the process start
				FileName = processName,
				Arguments = parameter,
				CreateNoWindow = false,
				ErrorDialog = false,
				LoadUserProfile = false,
				RedirectStandardError = getOutput,
				RedirectStandardInput = false,
				RedirectStandardOutput = getOutput,
				StandardErrorEncoding = getOutput ? System.Text.Encoding.GetEncoding(850) : null,
				StandardOutputEncoding = getOutput ? System.Text.Encoding.GetEncoding(850) : null,
				UseShellExecute = !getOutput, // true for collection output only
				WindowStyle = hideWindow ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal,
				WorkingDirectory = ""
			};

			try {
				// start process
				Process process = Process.Start(startInfo);
				// want to get output? Important: read output and error before waiting for process exit to avoid deadlock situation
				if (getOutput) output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
				// wait for completion of process if selected
				if (waitForExit || getOutput) process.WaitForExit(3000);
				process.Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Cannot start process '" + processName + "'.\r\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}

			return output;
		}
		#endregion

	} // end of CustomWindow

	#region ini file handling
	public class IniFile
	{
		string iniFilePath;

		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

		public IniFile(string iniPath = null)
		{
			iniFilePath = new FileInfo(iniPath ?? Assembly.GetExecutingAssembly().GetName().Name + ".ini").FullName.ToString();
		}

		public string Read(string section, string key)
		{
			var retVal = new StringBuilder(8192);
			GetPrivateProfileString(section ?? "Default", key, "", retVal, 8192, iniFilePath);
			return retVal.ToString();
		}

		public void Write(string section, string key, string value)
		{
			WritePrivateProfileString(section ?? "Default", key, value, iniFilePath);
		}

		public void DeleteKey(string section, string key)
		{
			Write(key, null, section ?? "Default");
		}

		public void DeleteSection(string section)
		{
			Write(null, null, section ?? "Default");
		}

		public bool KeyExists(string section, string key)
		{
			return Read(key, section).Length > 0;
		}
	} // end of IniFile
	#endregion

	public class Program
	{
		// WPF requires STA model, since C# default to MTA threading, the following directive is mandatory
		[STAThread]
		public static void Main()
		{
			// XAML string defining the window controls
			string strXAML = @"
<local:CustomWindow
	xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
	xmlns:local=""clr-namespace:***NAMESPACE***;assembly=***ASSEMBLY***""
	xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
	x:Name=""Window"" Title=""***ASSEMBLY***"" WindowStartupLocation=""Manual""
	Background=""#FFE8E8E8"" Width=""292"" Height=""544"" ShowInTaskbar=""True"">
	<Grid>
		<Grid.ColumnDefinitions>
			<ColumnDefinition Width=""auto"" />
			<ColumnDefinition Width=""auto"" />
		</Grid.ColumnDefinitions>
		<Grid.RowDefinitions>
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
			<RowDefinition Height=""auto"" />
		</Grid.RowDefinitions>

		<Button x:Name=""Clipboard"" Background=""#FFD0D0D0"" Margin=""8,8,8,0"" Height=""18"" Width=""72"" Content=""Clipboard"" ToolTip=""Send content of clipboard (Ctrl-^)"" Grid.Row=""0"" Grid.Column=""0""
				Click=""ClipboardButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<WrapPanel Grid.Row=""0"" Grid.Column=""1"" >
		<Button x:Name=""RescanConfig"" Background=""#FFD0D0D0"" HorizontalAlignment=""Left"" Margin=""0,8,10,0"" Height=""18"" Width=""84"" Content=""Rescan config"" ToolTip=""Rescan configuration""
				Click=""RescanConfigButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<Button x:Name=""EditConfig"" Background=""#FFD0D0D0"" HorizontalAlignment=""Right"" Margin=""2,8,10,0"" Height=""18"" Width=""84"" Content=""Edit config"" ToolTip=""Edit configuration""
				Click=""EditFileButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		</WrapPanel>

		<Button x:Name=""SendText1"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-1)"" Grid.Row=""1"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextToSend1"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""1"" Grid.Column=""1"" />

		<Button x:Name=""SendText2"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-2)"" Grid.Row=""2"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextToSend2"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""2"" Grid.Column=""1"" />

		<Button x:Name=""SendText3"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-3)"" Grid.Row=""3"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextToSend3"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""3"" Grid.Column=""1"" />

		<Button x:Name=""SendText4"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-4)"" Grid.Row=""4"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextToSend4"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""4"" Grid.Column=""1"" />

		<Button x:Name=""SendText5"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-5)"" Grid.Row=""5"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextToSend5"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""5"" Grid.Column=""1"" />

		<Button x:Name=""SendText6"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-6)"" Grid.Row=""6"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextToSend6"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""6"" Grid.Column=""1"" />

		<Button x:Name=""SendText7"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-7)"" Grid.Row=""7"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextToSend7"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""7"" Grid.Column=""1"" />

		<Button x:Name=""SendText8"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-8)"" Grid.Row=""8"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextToSend8"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""8"" Grid.Column=""1"" />

		<Button x:Name=""SendText9"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-9)"" Grid.Row=""9"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextToSend9"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""9"" Grid.Column=""1"" />

		<Button x:Name=""SendText0"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-0)"" Grid.Row=""10"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextToSend0"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""10"" Grid.Column=""1"" />

		<Button x:Name=""SendFile"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send file"" ToolTip=""Send content of file (Ctrl-ß)"" Grid.Row=""11"" Grid.Column=""0""
				Click=""SendFileButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<CheckBox x:Name=""MultiLines"" IsChecked=""False"" Height=""18"" Width=""72"" HorizontalAlignment=""Right"" Margin=""0,4,16,0"" ToolTip=""Send text in multiple sendkeys commands if multiple lines of text"" 
				Grid.Row=""11"" Grid.Column=""1"">Multilines</CheckBox>

		<Button x:Name=""EditFile"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Edit file"" ToolTip=""Edit content of file (Ctrl-´)"" Grid.Row=""12"" Grid.Column=""0""
				Click=""EditFileButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<WrapPanel Grid.Row=""12"" Grid.Column=""1"" >
			<TextBox x:Name=""FileToSend"" Height=""18"" Width=""156"" AllowDrop=""True"" ToolTip=""Path and name of the file with text to send""
				PreviewDragEnter=""TextBox_PreviewDragOver"" PreviewDragOver=""TextBox_PreviewDragOver"" PreviewDrop=""TextBox_PreviewDrop"">C:\Daten\Liste.txt</TextBox>
			<Button x:Name=""FileToSendPicker"" Background=""#FFD0D0D0"" Height=""18"" Width=""24"" Content=""..."" ToolTip=""File picker for file to send""
				Click=""FilePicker_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		</WrapPanel>

		<Button x:Name=""SendCommand"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Command"" ToolTip=""Send output of command (Ctrl-Shift-^)"" Grid.Row=""13"" Grid.Column=""0""
				Click=""CommandButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""CommandToSend"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Command to execute"" Grid.Row=""13"" Grid.Column=""1"" />

		<Button x:Name=""SendTextShift1"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-Shift-1)"" Grid.Row=""14"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextShiftToSend1"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""14"" Grid.Column=""1"" />

		<Button x:Name=""SendTextShift2"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-Shift-2)"" Grid.Row=""15"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextShiftToSend2"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""15"" Grid.Column=""1"" />

		<Button x:Name=""SendTextShift3"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-Shift-3)"" Grid.Row=""16"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextShiftToSend3"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""16"" Grid.Column=""1"" />

		<Button x:Name=""SendTextShift4"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-Shift-4)"" Grid.Row=""17"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextShiftToSend4"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""17"" Grid.Column=""1"" />

		<Button x:Name=""SendTextShift5"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-Shift-5)"" Grid.Row=""18"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextShiftToSend5"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""18"" Grid.Column=""1"" />

		<Button x:Name=""SendTextShift6"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-Shift-6)"" Grid.Row=""19"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextShiftToSend6"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""19"" Grid.Column=""1"" />

		<Button x:Name=""SendTextShift7"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-Shift-7)"" Grid.Row=""20"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextShiftToSend7"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""20"" Grid.Column=""1"" />

		<Button x:Name=""SendTextShift8"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-Shift-8)"" Grid.Row=""21"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextShiftToSend8"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""21"" Grid.Column=""1"" />

		<Button x:Name=""SendTextShift9"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-Shift-9)"" Grid.Row=""22"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextShiftToSend9"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""22"" Grid.Column=""1"" />

		<Button x:Name=""SendTextShift0"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-Shift-0)"" Grid.Row=""23"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextShiftToSend0"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""23"" Grid.Column=""1"" />

		<Button x:Name=""SendTextShiftSZ"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-Shift-ß)"" Grid.Row=""24"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextShiftToSendSZ"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""24"" Grid.Column=""1"" />

		<Button x:Name=""SendTextShiftAccent"" Background=""#FFD0D0D0"" Margin=""8,0,8,0"" Height=""18"" Width=""72"" Content=""Send text"" ToolTip=""Send content of text field (Ctrl-Shift-´)"" Grid.Row=""25"" Grid.Column=""0""
				Click=""TextButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		<TextBox x:Name=""TextShiftToSendAccent"" Height=""18"" Width=""180"" Margin=""0,0,10,0"" ToolTip=""Text to send"" Grid.Row=""25"" Grid.Column=""1"" />

		<CheckBox x:Name=""HotkeyModifier"" IsChecked=""False"" Height=""18"" Width=""72"" Margin=""-2,4,0,0"" ToolTip=""Hotkey modifier CTRL (unchecked) or ALT (checked)"" Grid.Row=""26"" Grid.Column=""0""
				Checked=""HotkeyModifier_Click"" Unchecked=""HotkeyModifier_Click"" >AltHotkey</CheckBox>
		<WrapPanel Grid.Row=""26"" Grid.Column=""1"" >
			<Label Margin=""0,0,0,0"" >Wait (ms):</Label>
			<TextBox x:Name=""Wait"" Height=""18"" Width=""36"" Margin=""-6,0,0,0"" ToolTip=""Wait in milliseconds after click before start of sending text"">1500</TextBox>
			<Button x:Name=""HelpToSendkeys"" Background=""#FFD0D0D0"" HorizontalAlignment=""Right"" Margin=""3,0,10,0"" Height=""18"" Width=""84"" Content=""Sendkeys help"" ToolTip=""Show help for sendkeys.exe""
				Click=""HelpButton_Click"" MouseEnter=""Button_MouseEnter"" MouseLeave=""Button_MouseLeave"" />
		</WrapPanel>
	</Grid>
</local:CustomWindow>";

			// generate WPF object tree
			CustomWindow objWindow;
			try
			{	// assign XAML root object
				objWindow = CustomWindow.LoadWindowFromXaml(strXAML.Replace("***ASSEMBLY***", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name).Replace("***NAMESPACE***", System.Reflection.Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace));

				// arrange window at right border
				objWindow.Left = SystemParameters.PrimaryScreenWidth - objWindow.Width;
				objWindow.Top = 200.0;
			}
			catch (Exception ex)
			{ // on error in XAML definition XamlReader sometimes generates an exception
				MessageBox.Show("Error creating the window objects from XAML description\r\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// and show window
			objWindow.ShowDialog();
		}
	} // end of Program

}  // end of WPFApplication
