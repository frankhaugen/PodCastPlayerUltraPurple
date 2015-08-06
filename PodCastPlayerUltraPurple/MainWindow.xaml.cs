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

using System.Xml;
using System.Xml.Linq;
using System.Media;

using System.ComponentModel;

using System.Net;


using NAudio;
using NAudio.Wave;

namespace PodCastPlayerUltraPurple
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	/// 

	//public delegate void MyEventHandler();


	public partial class MainWindow : Window
	{
		//Declarations required for audio out and the MP3 stream
		IWavePlayer waveOutDevice;
		AudioFileReader audioFileReader;
		
		public string FileUrl = @"http://castracker.com/t.html?f=http%3A%2F%2Ffeeds.unusualradio.net%2F%7Er%2FTheGadgetCrowd%2F%7E5%2FmepHttm-JAA%2F009.mp3";
		public WebClient webClient = new WebClient();

		//public static event MyEventHandler _show;

		public MainWindow()
		{
			InitializeComponent();

			//PopulateList();
			AddTabs();
		}

		private void AddTabs()
		{
			XDocument podcasts = XDocument.Load("Podcastlist.xml");

			XElement casts = podcasts.Element("podcasts");

			//List<XElement> castlist = new List<XElement>();
			IEnumerable<XElement> castlist = from pcs in casts.Elements("podcast").Descendants() select pcs;

			foreach (XElement cast in castlist)
			{
				XAttribute castName = cast.Attribute("name");
				XAttribute castID = cast.Attribute("id");
				XAttribute castURL = cast.Attribute("url");
				XAttribute castFavorite = cast.Attribute("favorite");

				ListBox lstBox = new ListBox();
				lstBox.Name = "listbox_" + castName;

				var posts = new RssFeedReader().ReadFeed(castURL.Value);
				int numofposts = posts.Count();

				foreach (var item in posts.ToList())
				{
					TextBlock txtBlock = new TextBlock();
					txtBlock.Text = item.Description;
					txtBlock.TextWrapping = TextWrapping.Wrap;

					Separator sep = new Separator();
					sep.Margin = new Thickness(10, 0, 0, 0);
					sep.Width = 500;

					Label lbl = new Label();
					lbl.Height = 0;
					lbl.Visibility = Visibility.Hidden;
					lbl.Content = item.URL;

					Button btn = new Button();
					btn.Name = "btnDownloadAndPlayFile_" + numofposts.ToString();
					btn.Tag = item.URL;
					btn.Click += new RoutedEventHandler(DownloadEpisode_Click);
					btn.Content = "Download";

					Expander exp = new Expander();
					exp.Header = numofposts.ToString() + " - " + item.Title + "\n" + item.PublishedDate;
					exp.Content = txtBlock;

					StackPanel panelVertical = new StackPanel();
					panelVertical.Children.Add(lbl);
					panelVertical.Children.Add(btn);
					panelVertical.Children.Add(exp);
					panelVertical.Children.Add(sep);

					lstBox.Items.Add(panelVertical);

					numofposts--;
				}

				TabItem tabitem = new TabItem();
				tabitem.Content = lstBox;

				Podcastlist.Items.Add(tabitem);
			}

		}

		private void PopulateList()
		{
			var posts = new RssFeedReader().ReadFeed(@"http://www.pwop.com/feed.aspx?show=dotnetrocks&filetype=master");

			int numofposts = posts.Count();
			
			foreach (var item in posts.ToList())
			{
				TextBlock txtBlock = new TextBlock();
				txtBlock.Text = item.Description;
				txtBlock.TextWrapping = TextWrapping.Wrap;
				
				Separator sep = new Separator();
				sep.Margin = new Thickness(10, 0, 0, 0);
				sep.Width = 500;

				Label lbl = new Label();
				lbl.Height = 0;
				lbl.Visibility = Visibility.Hidden;
				lbl.Content = item.URL;

				Button btn = new Button();
				btn.Name = "btnDownloadAndPlayFile_" + numofposts.ToString();
				btn.Tag = item.URL;
				btn.Click += new RoutedEventHandler(DownloadEpisode_Click);
				btn.Content = "Download";

				Expander exp = new Expander();
				exp.Header = numofposts.ToString() + " - " + item.Title + "\n" + item.PublishedDate;
				exp.Content = txtBlock;

				StackPanel panelVertical = new StackPanel();
				panelVertical.Children.Add(lbl);
				panelVertical.Children.Add(btn);
				panelVertical.Children.Add(exp);
				panelVertical.Children.Add(sep);
				
				lstBoxAlpha.Items.Add(panelVertical);

				numofposts--;
			}
		}

		private void DownloadEpisode_Click(object sender, System.EventArgs e)
		{
			Button btn = (Button)sender;

			webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
			webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
			webClient.DownloadFileAsync(new Uri(btn.Tag.ToString()), @"playthis.mp3");

			btn.Content = "Downloaded";
			btn.IsEnabled = false;

			MessageBox.Show(btn.Name);
		}

		private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			progressBar.Value = e.ProgressPercentage;
		}

		private void Completed(object sender, AsyncCompletedEventArgs e)
		{
			MessageBox.Show("Download completed!");
		}

		private void btnDownloadURL_Click(object sender, RoutedEventArgs e)
		{
			webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
			webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
			webClient.DownloadFileAsync(new Uri(FileUrl), @"playthis.mp3");
		}

		private void btnPlay_Click(object sender, RoutedEventArgs e)
		{

			waveOutDevice = new WaveOut();
			audioFileReader = new AudioFileReader(@"playthis.mp3");
			waveOutDevice.Init(audioFileReader);
			waveOutDevice.Play();
		}

		private void btnPause_Click(object sender, RoutedEventArgs e)
		{
			waveOutDevice.Pause();
		}

		private void btnStop_Click(object sender, RoutedEventArgs e)
		{
			waveOutDevice.Stop();
		}
	}

	public class RssFeedReader
	{
		public IEnumerable<Post> ReadFeed(string url)
		{
			var rssFeed = XDocument.Load(url);

			var posts = from item in rssFeed.Descendants("item")
						select new Post
						{
							Title = item.Element("title").Value,
							Description = item.Element("description").Value,
							PublishedDate = item.Element("pubDate").Value,
                            URL = item.Element("enclosure").Attribute("url").Value
						};
			return posts;
		}
	}

	public class Post
	{
		public string PublishedDate;
		public string Description;
		public string Title;
		public string URL;
	}
}
