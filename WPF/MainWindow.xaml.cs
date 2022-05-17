using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using MailKit.Net.Smtp;
using Microsoft.Win32;
using MimeKit;

//using Timer = System.Windows.Threading.DispatcherTimer;

namespace WPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private List<string> _listOfFilePaths = new List<string>();

		public MainWindow()
		{
			InitializeComponent();
			Title = "EMAiL";
			MinHeight = Height / 3;
			MinWidth = Width / 3;
		}

		private void ButtonClick_Settings(object sender, RoutedEventArgs e)
		{
			GridStart.Visibility = Visibility.Hidden;
			GridSettings.Visibility = Visibility.Visible;
			LabelLogin.Content = Settings.Default.Login;
			LabelPassword.Content = Settings.Default.Password;
			LabelName.Content = Settings.Default.Name;
		}
		private void ButtonClick_BackToStart(object sender, RoutedEventArgs e)
		{
			LabelSettingsResult.Visibility = Visibility.Hidden;
			GridStart.Visibility = Visibility.Visible;
			GridWriteLetter.Visibility = Visibility.Hidden;
			GridContacts.Visibility = Visibility.Hidden;
			GridSettings.Visibility = Visibility.Hidden;
			_listOfFilePaths = new List<string>();
		}
		private void ButtonClick_ConfirmSettings(object sender, RoutedEventArgs e)
		{
			if (TextBoxLogin.Text != "")
			{
				Settings.Default.Login = TextBoxLogin.Text;
				Settings.Default.Save();
				TextBoxLogin.Text = "";
			}

			if (TextBoxPassword.Text != "")
			{
				Settings.Default.Password = TextBoxPassword.Text;
				Settings.Default.Save();
				TextBoxPassword.Text = "";
			}

			if (TextBoxName.Text != "")
			{
				Settings.Default.Name = TextBoxName.Text;
				Settings.Default.Save();
				TextBoxName.Text = "";
			}
			LabelLogin.Content = Settings.Default.Login;
			LabelPassword.Content = Settings.Default.Password;
			LabelName.Content = Settings.Default.Name;
			GridSettings.UpdateLayout();
			LabelSettingsResult.Visibility = Visibility.Visible;
		}
		private void ButtonClick_Check(object sender, RoutedEventArgs e)
		{
			try
			{
				using (SmtpClient smtp = new SmtpClient())
				{
					try
					{
						smtp.Connect("smtp.yandex.ru", 465, true);
						smtp.Authenticate(Settings.Default.Login, Settings.Default.Password);
						MessageBox.Show($"All okey!");
					}
					catch
					{
						throw new Exception("Fail with login or password");
					}
				}
			}
			catch (Exception exception)
			{
				MessageBox.Show($"Error: {exception.Message}", "Ошибка");
			}
		}

		private void ButtonClick_WriteLetter(object sender, RoutedEventArgs e)
		{
			GridStart.Visibility = Visibility.Hidden;
			GridWriteLetter.Visibility = Visibility.Visible;
			WriteListTo();
			WriteListOfFiles();
		}
		private void WriteListTo()
		{
			ListBoxTo.Items.Clear();
			if (Settings.Default.AddressesTo == "")
				return;
			List<string> addresses = Settings.Default.AddressesTo.Split(" ").ToList();

			foreach (string address in addresses)
			{
				ListBoxTo.Items.Add(new CheckBox()
				{
					Content = $" {address}",
					FontSize = 30,
					FontFamily = new FontFamily("Bahnschrift Light Condensed"),
				});
			}
		}
		private void ButtonClick_OpenFile(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Multiselect = true,
				InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
			};
			if (openFileDialog.ShowDialog() != true) return;
			Stream[] streams = openFileDialog.OpenFiles();
			foreach (Stream stream in streams)
			{
				FileStream fileStream = stream as FileStream;
				if (fileStream != null)
				{
					_listOfFilePaths.Add(fileStream.Name);
				}
			}
			WriteListOfFiles();
		}
		private void ButtonClick_DeleteFile(object sender, RoutedEventArgs e)
		{
			Button button = (Button) sender;
			string path = button.Tag.ToString();
			if (path != "")
				_listOfFilePaths.Remove(path);
			WriteListOfFiles();
		}
		private void WriteListOfFiles()
		{
			ListBoxAddedFiles.Items.Clear();
			if(_listOfFilePaths.Count == 0) return;
			foreach (string path in _listOfFilePaths)
			{
				Grid grid = new Grid()
				{
					Width = 240
				};
				Button button = new Button()
				{
					Content = "X",
					Tag = path.ToString(),
					Width = 20,
					Background = Brushes.Red,
					Foreground = Brushes.White,
					Margin = new Thickness(5, 5, 215, 0),
					Height = 20,
					VerticalAlignment = VerticalAlignment.Top
				};
				button.Click += ButtonClick_DeleteFile;
				ListBoxItem listBoxItem = new ListBoxItem()
				{
					Content = path.Substring(path.LastIndexOf("\\")+1),
					FontSize = 20,
					Margin = new Thickness(26, 0, 0, 0)
				};
				grid.Children.Add(button);
				grid.Children.Add(listBoxItem);
				ListBoxAddedFiles.Items.Add(grid);
			}
		}
		private void ButtonClick_Send(object sender, RoutedEventArgs e)
		{
			List<string> addressesTo = new List<string>();
			foreach (CheckBox checkBox in ListBoxTo.Items)
			{
				if(checkBox.IsChecked == true)
					addressesTo.Add(checkBox.Content.ToString());
			}
			if (addressesTo.Count == 0)
			{
				MessageBox.Show("Нету адресов получателя!");
				return;
			}

			string subject = TextBoxSubject.Text;
			if (subject == "")
			{
				MessageBox.Show("Нету темы письма!");
				return;
			}
			string text = TextBoxText.Text;
			text = text.Replace("\r\n", "<br>");
			if (text == "")
			{
				MessageBox.Show("Нету текста!");
				return;
			}

			SendMailAsync(addressesTo, subject, text, _listOfFilePaths).GetAwaiter();
		}

		private void ButtonClick_Contacts(object sender, RoutedEventArgs e)
		{
			GridStart.Visibility = Visibility.Hidden;
			GridContacts.Visibility = Visibility.Visible;
			WriteListContacts();
		}
		private void ButtonClick_DeleteContact(object sender, RoutedEventArgs e)
		{
			if (ListBoxContacts.SelectedItem != null)
			{
				List<string> addresses = Settings.Default.AddressesTo.Split(" ").ToList();
				ListBoxItem item = (ListBoxItem) ListBoxContacts.SelectedItem;
				addresses.Remove(item.Content.ToString());
				ChangeContacts(addresses);
				WriteListContacts();
			}
		}
		private void ButtonClick_AddContact(object sender, RoutedEventArgs e)
		{
			List<string> addresses;
			if (Settings.Default.AddressesTo == "")
				addresses = new List<string>();
			else
				addresses = Settings.Default.AddressesTo.Split(" ").ToList();
			addresses.Add(TextBoxNewContact.Text);
			ChangeContacts(addresses);
			TextBoxNewContact.Text = "";
			WriteListContacts();
		}
		private void WriteListContacts()
		{
			ListBoxContacts.Items.Clear();
			if (Settings.Default.AddressesTo == "")
				return;
			List<string> addresses = Settings.Default.AddressesTo.Split(" ").ToList();

			foreach (string address in addresses)
			{
				ListBoxContacts.Items.Add(new ListBoxItem()
				{
					Content = address,
					FontSize = 30,
					FontFamily = new FontFamily("Bahnschrift Light Condensed"),
				});
			}
		}
		private void ChangeContacts(List<string> addresses)
		{
			string str = "";
			if(addresses.Count != 0)
			{
				for (int i = 0; i < addresses.Count; i++)
				{
					if (i != addresses.Count - 1)
						str += $"{addresses[i]} ";
					else
						str += addresses[i];
				}
			}
			Settings.Default.AddressesTo = str;
			Settings.Default.Save();
		}

		private async Task SendMailAsync(List<string> addressesTo, string subject, string text, List<string> attachments)
		{
			try
			{
				using (SmtpClient smtp = new SmtpClient())
				{
					try
					{
						await smtp.ConnectAsync("smtp.yandex.ru", 465, true);
					}
					catch
					{
						throw new Exception("Connect to smtp.yandex.ru is fail!");
					}

					try
					{
						await smtp.AuthenticateAsync(Settings.Default.Login, Settings.Default.Password);
					}
					catch
					{
						throw new Exception("Fail with login or password");
					}

					BodyBuilder bodyBuilder = new BodyBuilder
						{HtmlBody = text};
					if (attachments.Count != 0)
					{
						foreach (string path in attachments)
						{
							await bodyBuilder.Attachments.AddAsync(path);
						}
					}

					MimeMessage message = new MimeMessage()
					{
						Subject = subject,
						Body = bodyBuilder.ToMessageBody(),
					};

					foreach (string s in addressesTo)
					{
						message.To.Add(MailboxAddress.Parse(s));
					}

					message.From.Add(new MailboxAddress(Settings.Default.Name, $"{Settings.Default.Login}@yandex.ru"));
					await smtp.SendAsync(message);
				}
				MessageBox.Show("Письмо отправленно!");
				foreach (CheckBox checkBox in ListBoxTo.Items)
					checkBox.IsChecked = false;
				TextBoxSubject.Text = "";
				TextBoxText.Text = "";
				ListBoxAddedFiles.Items.Clear();
			}
			catch (Exception exception)
			{
				MessageBox.Show($"Error: {exception.Message}", "Ошибка");
			}
		}
		private static DependencyObject FindDescendant(DependencyObject parent, string name)
		{
			// See if this object has the target name.
			FrameworkElement element = parent as FrameworkElement;
			if ((element != null) && (element.Name == name)) return parent;

			// Recursively check the children.
			int num_children = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < num_children; i++)
			{
				// See if this child has the target name.
				DependencyObject child = VisualTreeHelper.GetChild(parent, i);
				DependencyObject descendant = FindDescendant(child, name);
				if (descendant != null) return descendant;
			}

			// We didn't find a descendant with the target name.
			return null;
		}

		
	}
}
