using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.UI;
using Tobi.Common.UI.XAML;
using urakawa.core;
using urakawa.data;
using urakawa.exception;
using urakawa.media;
using urakawa.media.data.audio;
using urakawa.media.data.image;
using urakawa.media.data.video;
using urakawa.media.timing;
using urakawa.property.xml;
using urakawa.xuk;
using Colors = System.Windows.Media.Colors;

#if ENABLE_WPF_MEDIAKIT
using WPFMediaKit.DirectShow.Controls;
using MediaState = System.Windows.Controls.MediaState;
using WPFMediaKit.DirectShow.MediaPlayers;
#endif //ENABLE_WPF_MEDIAKIT

namespace Tobi.Plugin.DocumentPane
{
    public partial class XukToFlowDocument
    {
        private TextElement walkBookTreeAndGenerateFlowDocument_video(TreeNode node, TextElement parent, QualifiedName qname, string textMedia)
        {
            if (node.Children.Count != 0 || textMedia != null && !String.IsNullOrEmpty(textMedia))
            {
#if DEBUG
                Debugger.Break();
#endif
                throw new Exception("Node has children or text exists when processing video ??");
            }

            XmlProperty xmlProp = node.GetProperty<XmlProperty>();

            AbstractVideoMedia videoMedia = node.GetVideoMedia();
            var videoMedia_ext = videoMedia as ExternalVideoMedia;
            var videoMedia_man = videoMedia as ManagedVideoMedia;

            string dirPath = Path.GetDirectoryName(m_TreeNode.Presentation.RootUri.LocalPath);

            string videoPath = null;

            if (videoMedia_ext != null)
            {

#if DEBUG
                Debugger.Break();
#endif //DEBUG

                //http://blogs.msdn.com/yangxind/archive/2006/11/09/don-t-use-net-system-uri-unescapedatastring-in-url-decoding.aspx

                videoPath = Path.Combine(dirPath, Uri.UnescapeDataString(videoMedia_ext.Src));
            }
            else if (videoMedia_man != null)
            {
#if DEBUG
                XmlAttribute srcAttr = xmlProp.GetAttribute("src");

                DebugFix.Assert(videoMedia_man.VideoMediaData.OriginalRelativePath == srcAttr.Value);
#endif //DEBUG
                var fileDataProv = videoMedia_man.VideoMediaData.DataProvider as FileDataProvider;

                if (fileDataProv != null)
                {
                    videoPath = fileDataProv.DataFileFullPath;
                }
            }

            if (videoPath == null || FileDataProvider.isHTTPFile(videoPath))
            {
#if DEBUG
                Debugger.Break();
#endif //DEBUG
                return parent;
            }

            var uri = new Uri(videoPath, UriKind.Absolute);


            string videoAlt = null;
            XmlAttribute altAttr = xmlProp.GetAttribute("alt");
            if (altAttr != null)
            {
                videoAlt = altAttr.Value;
            }



            bool parentHasBlocks = parent is TableCell
                                   || parent is Section
                                   || parent is Floater
                                   || parent is Figure
                                   || parent is ListItem;

            var videoPanel = new StackPanel();
            videoPanel.Orientation = Orientation.Vertical;
            //videoPanel.LastChildFill = true;
            if (!string.IsNullOrEmpty(videoAlt))
            {
                var tb = new TextBlock(new Run(videoAlt))
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };
                videoPanel.Children.Add(tb);
            }
            //videoPanel.Children.Add(mediaElement);


            var slider = new Slider();
            slider.Focusable = false;
            slider.TickPlacement = TickPlacement.None;
            slider.IsMoveToPointEnabled = true;
            slider.Minimum = 0;
            slider.Maximum = 100;
            slider.Visibility = Visibility.Hidden;

            videoPanel.Children.Add(slider);


            var timeLabel = new TextBox();
            timeLabel.Focusable = false;
            //timeLabel.IsEnabled = false;
            timeLabel.TextWrapping = TextWrapping.NoWrap;
            //timeLabel.TextTrimming = TextTrimming.None;
            timeLabel.HorizontalAlignment = HorizontalAlignment.Stretch;
            timeLabel.TextAlignment = TextAlignment.Center;
            timeLabel.Visibility = Visibility.Hidden;

            videoPanel.Children.Add(timeLabel);

            videoPanel.HorizontalAlignment = HorizontalAlignment.Center;
            videoPanel.VerticalAlignment = VerticalAlignment.Top;

            var panelBorder = new Border();
            panelBorder.Child = videoPanel;
            panelBorder.Padding = new Thickness(4);
            panelBorder.BorderBrush = ColorBrushCache.Get(Settings.Default.Document_Color_Font_Audio);
            panelBorder.BorderThickness = new Thickness(2.0);


            if (parentHasBlocks)
            {
                Block vidContainer = new BlockUIContainer(panelBorder);

                setTag(vidContainer, node);

                addBlock(parent, vidContainer);
            }
            else
            {
                Inline vidContainer = new InlineUIContainer(panelBorder);

                setTag(vidContainer, node);

                addInline(parent, vidContainer);
            }


            MediaElement medElement_WINDOWS_MEDIA_PLAYER = null;
#if ENABLE_WPF_MEDIAKIT
            MediaUriElement medElement_MEDIAKIT_DIRECTSHOW = null;
#endif //ENABLE_WPF_MEDIAKIT

            m_FlowDoc.Loaded += new RoutedEventHandler(
                (o, e) =>
                {
#if ENABLE_WPF_MEDIAKIT
                    if (Common.Settings.Default.EnableMediaKit)
                    {
                        medElement_MEDIAKIT_DIRECTSHOW = new MediaUriElement();
                    }
                    else
#endif //ENABLE_WPF_MEDIAKIT
                    {
                        medElement_WINDOWS_MEDIA_PLAYER = new MediaElement();
                    }





#if ENABLE_WPF_MEDIAKIT
                    DebugFix.Assert((medElement_WINDOWS_MEDIA_PLAYER == null) == (medElement_MEDIAKIT_DIRECTSHOW != null));
#else  // DISABLE_WPF_MEDIAKIT
            DebugFix.Assert(medElement_WINDOWS_MEDIA_PLAYER!=null);
#endif //ENABLE_WPF_MEDIAKIT




                    if (medElement_WINDOWS_MEDIA_PLAYER != null)
                    {
                        medElement_WINDOWS_MEDIA_PLAYER.Stretch = Stretch.Uniform;
                        medElement_WINDOWS_MEDIA_PLAYER.StretchDirection = StretchDirection.DownOnly;
                    }

#if ENABLE_WPF_MEDIAKIT
                    if (medElement_MEDIAKIT_DIRECTSHOW != null)
                    {
                        medElement_MEDIAKIT_DIRECTSHOW.Stretch = Stretch.Uniform;
                        medElement_MEDIAKIT_DIRECTSHOW.StretchDirection = StretchDirection.DownOnly;
                    }
#endif //ENABLE_WPF_MEDIAKIT



                    FrameworkElement mediaElement = null;
                    if (medElement_WINDOWS_MEDIA_PLAYER != null)
                    {
                        mediaElement = medElement_WINDOWS_MEDIA_PLAYER;
                    }
                    else
                    {
                        mediaElement = medElement_MEDIAKIT_DIRECTSHOW;
                    }

                    mediaElement.Focusable = false;



                    XmlAttribute srcW = xmlProp.GetAttribute("width");
                    if (srcW != null)
                    {
                        double ww = Double.Parse(srcW.Value);
                        mediaElement.Width = ww;
                    }
                    XmlAttribute srcH = xmlProp.GetAttribute("height");
                    if (srcH != null)
                    {
                        double hh = Double.Parse(srcH.Value);
                        mediaElement.Height = hh;
                    }

                    mediaElement.HorizontalAlignment = HorizontalAlignment.Center;
                    mediaElement.VerticalAlignment = VerticalAlignment.Top;

                    if (!string.IsNullOrEmpty(videoAlt))
                    {
                        mediaElement.ToolTip = videoAlt;
                    }

                    videoPanel.Children.Insert(1, mediaElement);

                    var actionMediaFailed = new Action<string>(
                        (str) =>
                        {
                            m_DocumentPaneView.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                (Action)(() =>
                                {
                                    var label = new TextBlock(new Run(str));

                                    label.TextWrapping = TextWrapping.Wrap;
                                    //label.Height = 150;

                                    var border = new Border();
                                    border.Child = label;
                                    border.BorderBrush = ColorBrushCache.Get(Colors.Red);
                                    border.BorderThickness = new Thickness(2.0);

                                    videoPanel.Children.Insert(1, border);

                                    slider.Visibility = Visibility.Hidden;
                                    timeLabel.Visibility = Visibility.Hidden;
                                }
                                ));
                        }
                        );


                    Action actionUpdateSliderFromVideoTime = null;
                    DispatcherTimer _timer = new DispatcherTimer();
                    _timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                    _timer.Stop();
                    _timer.Tick += (object oo, EventArgs ee) =>
                    {
                        actionUpdateSliderFromVideoTime.Invoke();
                    };



                    if (medElement_WINDOWS_MEDIA_PLAYER != null)
                    {
                        medElement_WINDOWS_MEDIA_PLAYER.ScrubbingEnabled = true;

                        medElement_WINDOWS_MEDIA_PLAYER.LoadedBehavior = MediaState.Manual;
                        medElement_WINDOWS_MEDIA_PLAYER.UnloadedBehavior = MediaState.Stop;


                        bool doNotUpdateVideoTimeWhenSliderChanges = false;
                        actionUpdateSliderFromVideoTime = new Action(() =>
                        {
                            TimeSpan? timeSpan = medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentTime;
                            double timeMS = timeSpan != null ? timeSpan.Value.TotalMilliseconds : 0;

                            //Console.WriteLine("UPDATE: " + timeMS);

                            //if (medElement_WINDOWS_MEDIA_PLAYER.NaturalDuration.HasTimeSpan
                            //    && timeMS >= medElement_WINDOWS_MEDIA_PLAYER.NaturalDuration.TimeSpan.TotalMilliseconds - 50)
                            //{
                            //    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Stop();
                            //}

                            doNotUpdateVideoTimeWhenSliderChanges = true;
                            slider.Value = timeMS;
                        });

                        medElement_WINDOWS_MEDIA_PLAYER.MediaFailed += new EventHandler<ExceptionRoutedEventArgs>(
                            (oo, ee) =>
                            {
                                //#if DEBUG
                                //                                Debugger.Break();
                                //#endif //DEBUG
                                //medElement_WINDOWS_MEDIA_PLAYER.Source
                                actionMediaFailed.Invoke(uri.ToString()
                                    + " \n("
                                    + (ee.ErrorException != null ? ee.ErrorException.Message : "ERROR!")
                                    + ")");
                            }
                            );



                        medElement_WINDOWS_MEDIA_PLAYER.MediaOpened += new RoutedEventHandler(
                            (oo, ee) =>
                            {
                                slider.Visibility = Visibility.Visible;
                                timeLabel.Visibility = Visibility.Visible;

                                double durationMS = medElement_WINDOWS_MEDIA_PLAYER.NaturalDuration.TimeSpan.TotalMilliseconds;
                                timeLabel.Text = Time.Format_Standard(medElement_WINDOWS_MEDIA_PLAYER.NaturalDuration.TimeSpan);

                                slider.Maximum = durationMS;


                                // freeze frame (poster)
                                if (medElement_WINDOWS_MEDIA_PLAYER.LoadedBehavior == MediaState.Manual)
                                {
                                    medElement_WINDOWS_MEDIA_PLAYER.IsMuted = true;

                                    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Begin();
                                    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();

                                    medElement_WINDOWS_MEDIA_PLAYER.IsMuted = false;

                                    slider.Value = 0.10;
                                }
                            }
                            );



                        medElement_WINDOWS_MEDIA_PLAYER.MediaEnded +=
                            new RoutedEventHandler(
                            (oo, ee) =>
                            {
                                _timer.Stop();

                                medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Stop();

                                actionUpdateSliderFromVideoTime.Invoke();
                            }
                            );


                        medElement_WINDOWS_MEDIA_PLAYER.MouseDown += new MouseButtonEventHandler(
                                (oo, ee) =>
                                {
                                    if (medElement_WINDOWS_MEDIA_PLAYER.LoadedBehavior != MediaState.Manual)
                                    {
                                        return;
                                    }

                                    if (ee.ChangedButton == MouseButton.Left)
                                    {
                                        bool wasPlaying = false;
                                        bool wasStopped = false;

                                        //Is Active
                                        if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentState == ClockState.Active)
                                        {
                                            //Is Paused
                                            if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentGlobalSpeed == 0.0)
                                            {
                                            }
                                            else //Is Playing
                                            {
                                                wasPlaying = true;
                                                medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                                            }
                                        }
                                        else if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentState == ClockState.Stopped)
                                        {
                                            wasStopped = true;
                                            //medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Begin();
                                            //medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                                        }

                                        double durationMS = medElement_WINDOWS_MEDIA_PLAYER.NaturalDuration.TimeSpan.TotalMilliseconds;
                                        double timeMS =
                                            medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentTime == null
                                            || !medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentTime.HasValue
                                            ? -1.0
                                            : medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentTime.Value.TotalMilliseconds;

                                        if (timeMS == -1.0 || timeMS >= durationMS)
                                        {
                                            slider.Value = 0.100;
                                        }

                                        if (!wasPlaying)
                                        {
                                            _timer.Start();
                                            if (wasStopped)
                                            {
                                                medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Begin();
                                            }
                                            else
                                            {
                                                medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Resume();
                                            }
                                        }
                                        else
                                        {
                                            _timer.Stop();
                                            medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                                            actionUpdateSliderFromVideoTime.Invoke();
                                        }
                                    }
                                    //else if (ee.ChangedButton == MouseButton.Right)
                                    //{
                                    //    _timer.Stop();

                                    //    //Is Active
                                    //    if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentState == ClockState.Active)
                                    //    {
                                    //        //Is Paused
                                    //        if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentGlobalSpeed == 0.0)
                                    //        {

                                    //        }
                                    //        else //Is Playing
                                    //        {
                                    //            medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                                    //        }
                                    //    }
                                    //    else if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentState == ClockState.Stopped)
                                    //    {
                                    //        medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Begin();
                                    //        medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                                    //    }

                                    //    //actionRefreshTime.Invoke();
                                    //    slider.Value = 0;
                                    //}
                                }
                                );

                        slider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(
                            (oo, ee) =>
                            {
                                var timeSpan = new TimeSpan(0, 0, 0, 0, (int)Math.Round(slider.Value));

                                if (doNotUpdateVideoTimeWhenSliderChanges || !_timer.IsEnabled)
                                {
                                    double durationMS = medElement_WINDOWS_MEDIA_PLAYER.NaturalDuration.TimeSpan.TotalMilliseconds;

                                    timeLabel.Text = String.Format(
                                        "{0} / {1}",
                                        Time.Format_Standard(timeSpan),
                                        Time.Format_Standard(medElement_WINDOWS_MEDIA_PLAYER.NaturalDuration.TimeSpan)
                                         );
                                }

                                if (doNotUpdateVideoTimeWhenSliderChanges)
                                {
                                    doNotUpdateVideoTimeWhenSliderChanges = false;
                                    return;
                                }

                                bool wasPlaying = false;

                                //Is Active
                                if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentState == ClockState.Active)
                                {
                                    //Is Paused
                                    if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentGlobalSpeed == 0.0)
                                    {

                                    }
                                    else //Is Playing
                                    {
                                        wasPlaying = true;
                                        medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                                    }
                                }
                                else if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentState == ClockState.Stopped)
                                {
                                    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Begin();
                                    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                                }

                                medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Seek(timeSpan, TimeSeekOrigin.BeginTime);

                                if (wasPlaying)
                                {
                                    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Resume();
                                }
                            });

                        bool wasPlayingBeforeDrag = false;
                        slider.AddHandler(Thumb.DragStartedEvent,
                            new DragStartedEventHandler(
                            (Action<object, DragStartedEventArgs>)(
                            (oo, ee) =>
                            {
                                wasPlayingBeforeDrag = false;

                                //Is Active
                                if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentState == ClockState.Active)
                                {
                                    //Is Paused
                                    if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentGlobalSpeed == 0.0)
                                    {

                                    }
                                    else //Is Playing
                                    {
                                        wasPlayingBeforeDrag = true;
                                        medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                                    }
                                }
                                else if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentState == ClockState.Stopped)
                                {
                                    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Begin();
                                    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                                }
                            })));

                        slider.AddHandler(Thumb.DragCompletedEvent,
                            new DragCompletedEventHandler(
                            (Action<object, DragCompletedEventArgs>)(
                            (oo, ee) =>
                            {
                                if (wasPlayingBeforeDrag)
                                {
                                    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Resume();
                                }
                                wasPlayingBeforeDrag = false;
                            })));
                    }












#if ENABLE_WPF_MEDIAKIT
                    if (medElement_MEDIAKIT_DIRECTSHOW != null)
                    {
                        bool doNotUpdateVideoTimeWhenSliderChanges = false;
                        actionUpdateSliderFromVideoTime = new Action(() =>
                        {
                            long timeVideo = medElement_MEDIAKIT_DIRECTSHOW.MediaPosition;

                            //if (timeMS >= medElement_MEDIAKIT_DIRECTSHOW.MediaDuration - 50 * 10000.0)
                            //{
                            //    medElement_MEDIAKIT_DIRECTSHOW.Stop();
                            //}


                            double timeMS = timeVideo / 10000.0;

                            //Console.WriteLine("UPDATE: " + timeMS);

                            doNotUpdateVideoTimeWhenSliderChanges = true;
                            slider.Value = timeMS;
                        });


                        medElement_MEDIAKIT_DIRECTSHOW.MediaFailed += new EventHandler<WPFMediaKit.DirectShow.MediaPlayers.MediaFailedEventArgs>(
                            (oo, ee) =>
                            {
                                //#if DEBUG
                                //                        Debugger.Break();
                                //#endif //DEBUG

                                //medElement_MEDIAKIT_DIRECTSHOW.Source
                                actionMediaFailed.Invoke(uri.ToString()
                                    + " \n("
                                    + (ee.Exception != null ? ee.Exception.Message : ee.Message)
                                    + ")");
                            }
                                );



                        medElement_MEDIAKIT_DIRECTSHOW.MediaOpened += new RoutedEventHandler(
                            (oo, ee) =>
                            {
                                long durationVideo = medElement_MEDIAKIT_DIRECTSHOW.MediaDuration;
                                if (durationVideo == 0)
                                {
                                    return;
                                }

                                //MediaPositionFormat mpf = medElement.CurrentPositionFormat;
                                //MediaPositionFormat.MediaTime
                                double durationMS = durationVideo / 10000.0;


                                slider.Visibility = Visibility.Visible;
                                timeLabel.Visibility = Visibility.Visible;

                                slider.Maximum = durationMS;

                                var durationTimeSpan = new TimeSpan(0, 0, 0, 0, (int)Math.Round(durationMS));
                                timeLabel.Text = Time.Format_Standard(durationTimeSpan);


                                // freeze frame (poster)
                                if (medElement_MEDIAKIT_DIRECTSHOW.LoadedBehavior == WPFMediaKit.DirectShow.MediaPlayers.MediaState.Manual)
                                {
                                    if (false)
                                    {
                                        double volume = medElement_MEDIAKIT_DIRECTSHOW.Volume;
                                        medElement_MEDIAKIT_DIRECTSHOW.Volume = 0;

                                        medElement_MEDIAKIT_DIRECTSHOW.Play();
                                        slider.Value = 0.10;
                                        medElement_MEDIAKIT_DIRECTSHOW.Pause();

                                        medElement_MEDIAKIT_DIRECTSHOW.Volume = volume;
                                    }
                                    else
                                    {
                                        medElement_MEDIAKIT_DIRECTSHOW.Pause();
                                        slider.Value = 0.10;
                                    }
                                }
                            }
                            );



                        medElement_MEDIAKIT_DIRECTSHOW.MediaEnded +=
                            new RoutedEventHandler(
                            (oo, ee) =>
                            {
                                _timer.Stop();
                                medElement_MEDIAKIT_DIRECTSHOW.Pause();
                                actionUpdateSliderFromVideoTime.Invoke();

                                // TODO: BaseClasses.cs in WPF Media Kit,
                                // MediaPlayerBase.OnMediaEvent
                                // ==> remove StopGraphPollTimer();
                                // in case EventCode.Complete.


                                //m_DocumentPaneView.Dispatcher.BeginInvoke(
                                //    DispatcherPriority.Background,
                                //    (Action)(() =>
                                //    {
                                //        //medElement_MEDIAKIT_DIRECTSHOW.BeginInit();
                                //        medElement_MEDIAKIT_DIRECTSHOW.Source = uri;
                                //        //medElement_MEDIAKIT_DIRECTSHOW.EndInit();
                                //    })
                                //    );
                            }
                            );


                        medElement_MEDIAKIT_DIRECTSHOW.MediaClosed +=
                            new RoutedEventHandler(
                            (oo, ee) =>
                            {
                                int debug = 1;
                            }
                            );

                        medElement_MEDIAKIT_DIRECTSHOW.MouseDown += new MouseButtonEventHandler(
                                (oo, ee) =>
                                {
                                    if (medElement_MEDIAKIT_DIRECTSHOW.LoadedBehavior != WPFMediaKit.DirectShow.MediaPlayers.MediaState.Manual)
                                    {
                                        return;
                                    }

                                    if (ee.ChangedButton == MouseButton.Left)
                                    {
                                        if (medElement_MEDIAKIT_DIRECTSHOW.IsPlaying)
                                        {
                                            _timer.Stop();
                                            medElement_MEDIAKIT_DIRECTSHOW.Pause();
                                            actionUpdateSliderFromVideoTime.Invoke();
                                        }
                                        else
                                        {
                                            _timer.Start();
                                            medElement_MEDIAKIT_DIRECTSHOW.Play();
                                        }


                                        double durationMS = medElement_MEDIAKIT_DIRECTSHOW.MediaDuration / 10000.0;
                                        double timeMS = medElement_MEDIAKIT_DIRECTSHOW.MediaPosition / 10000.0;

                                        if (timeMS >= durationMS)
                                        {
                                            slider.Value = 0.100;
                                        }
                                    }
                                    //else if (ee.ChangedButton == MouseButton.Right)
                                    //{
                                    //    _timer.Stop();
                                    //    medElement_MEDIAKIT_DIRECTSHOW.Pause();
                                    //    //actionRefreshTime.Invoke();
                                    //    slider.Value = 0;
                                    //}
                                }
                                );


                        slider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(
                            (oo, ee) =>
                            {
                                double timeMs = slider.Value;

                                if (doNotUpdateVideoTimeWhenSliderChanges || !_timer.IsEnabled)
                                {
                                    var timeSpan = new TimeSpan(0, 0, 0, 0, (int)Math.Round(timeMs));

                                    double durationMS = medElement_MEDIAKIT_DIRECTSHOW.MediaDuration / 10000.0;

                                    //MediaPositionFormat.MediaTime
                                    //MediaPositionFormat mpf = medElement.CurrentPositionFormat;

                                    timeLabel.Text = String.Format(
                                        "{0} / {1}",
                                        Time.Format_Standard(timeSpan),
                                        Time.Format_Standard(new TimeSpan(0, 0, 0, 0, (int)Math.Round(durationMS)))
                                         );
                                }

                                if (doNotUpdateVideoTimeWhenSliderChanges)
                                {
                                    doNotUpdateVideoTimeWhenSliderChanges = false;
                                    return;
                                }

                                bool wasPlaying = medElement_MEDIAKIT_DIRECTSHOW.IsPlaying;

                                if (wasPlaying)
                                {
                                    medElement_MEDIAKIT_DIRECTSHOW.Pause();
                                }

                                long timeVideo = (long)Math.Round(timeMs * 10000.0);
                                medElement_MEDIAKIT_DIRECTSHOW.MediaPosition = timeVideo;

                                DebugFix.Assert(medElement_MEDIAKIT_DIRECTSHOW.MediaPosition == timeVideo);

                                if (wasPlaying)
                                {
                                    medElement_MEDIAKIT_DIRECTSHOW.Play();
                                }
                            });

                        bool wasPlayingBeforeDrag = false;
                        slider.AddHandler(Thumb.DragStartedEvent,
                            new DragStartedEventHandler(
                            (Action<object, DragStartedEventArgs>)(
                            (oo, ee) =>
                            {
                                wasPlayingBeforeDrag = medElement_MEDIAKIT_DIRECTSHOW.IsPlaying;

                                if (wasPlayingBeforeDrag)
                                {
                                    medElement_MEDIAKIT_DIRECTSHOW.Pause();
                                }
                            })));


                        slider.AddHandler(Thumb.DragCompletedEvent,
                            new DragCompletedEventHandler(
                            (Action<object, DragCompletedEventArgs>)(
                            (oo, ee) =>
                            {
                                if (wasPlayingBeforeDrag)
                                {
                                    medElement_MEDIAKIT_DIRECTSHOW.Play();
                                }
                                wasPlayingBeforeDrag = false;
                            })));

                        //DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(
                        //    MediaSeekingElement.MediaPositionProperty,
                        //    typeof(MediaSeekingElement));
                        //if (dpd != null)
                        //{
                        //    dpd.AddValueChanged(medElement_MEDIAKIT_DIRECTSHOW, new EventHandler((o, e) =>
                        //    {
                        //        //actionRefreshTime.Invoke();

                        //        //if (!_timer.IsEnabled)
                        //        //{
                        //        //    _timer.Start();
                        //        //}
                        //    }));
                        //}

                    }
#endif //ENABLE_WPF_MEDIAKIT


                    if (medElement_WINDOWS_MEDIA_PLAYER != null)
                    {
                        var timeline = new MediaTimeline();
                        timeline.Source = uri;

                        medElement_WINDOWS_MEDIA_PLAYER.Clock = timeline.CreateClock(true) as MediaClock;

                        medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Stop();

                        //medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentTimeInvalidated += new EventHandler(
                        //(o, e) =>
                        //{
                        //    //actionRefreshTime.Invoke();
                        //    //if (!_timer.IsEnabled)
                        //    //{
                        //    //    _timer.Start();
                        //    //}
                        //});

                    }

#if ENABLE_WPF_MEDIAKIT
                    if (medElement_MEDIAKIT_DIRECTSHOW != null)
                    {
                        medElement_MEDIAKIT_DIRECTSHOW.BeginInit();

                        medElement_MEDIAKIT_DIRECTSHOW.Loop = false;
                        medElement_MEDIAKIT_DIRECTSHOW.VideoRenderer = VideoRendererType.VideoMixingRenderer9;

                        // seems to be a multiplicator of 10,000 to get milliseconds
                        medElement_MEDIAKIT_DIRECTSHOW.PreferedPositionFormat = MediaPositionFormat.MediaTime;


                        medElement_MEDIAKIT_DIRECTSHOW.LoadedBehavior = WPFMediaKit.DirectShow.MediaPlayers.MediaState.Manual;
                        medElement_MEDIAKIT_DIRECTSHOW.UnloadedBehavior = WPFMediaKit.DirectShow.MediaPlayers.MediaState.Stop;

                        try
                        {
                            medElement_MEDIAKIT_DIRECTSHOW.Source = uri;

                            medElement_MEDIAKIT_DIRECTSHOW.EndInit();
                        }
                        catch (Exception ex)
                        {
#if DEBUG
                            Debugger.Break();
#endif //DEBUG
                            ; // swallow (reported in MediaFailed)
                        }
                    }
#endif //ENABLE_WPF_MEDIAKIT
                });

            m_FlowDoc.Unloaded += new RoutedEventHandler(
                (o, e) =>
                {
                    if (medElement_WINDOWS_MEDIA_PLAYER != null)
                    {
                        medElement_WINDOWS_MEDIA_PLAYER.Close();
                    }

#if ENABLE_WPF_MEDIAKIT
                    if (medElement_MEDIAKIT_DIRECTSHOW != null)
                    {
                        medElement_MEDIAKIT_DIRECTSHOW.Close();
                    }
#endif //ENABLE_WPF_MEDIAKIT

                    videoPanel.Children.RemoveAt(1);
                });



            return parent;
        }
    }
}
