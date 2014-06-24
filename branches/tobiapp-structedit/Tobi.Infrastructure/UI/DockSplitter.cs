using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Diagnostics;

namespace Tobi.Infrastructure.UI //JaStDev.ControlFramework.Controls
{

   /// <summary>
   /// Determins how objects are resized.
   /// </summary>
   public enum DockSplitterResizeMode
   {
      /// <summary>
      /// In this mode, the first visible object before the splitter is resized.  All objects after the splitter remain the same size except the
      /// last object on the DockPanel (if it is set to 'LastChildFill = true) which will be resized to fill the remaining area of the DockPanel.
      /// </summary>
      Box,
      /// <summary>
      /// In this mode, the first visible object before and after the splitter are resized.  The object after the splitter gets the opposite amount
      /// so that all other objects on the DockPanel remain the same size.
      /// </summary>
      Adjacent
   }

   /// <summary>
   /// A splitter object to use on DockPanel objects.
   /// </summary>
   /// <remarks>
   /// <para>
   /// This class is a descendent of Thumb and provides splitter functionality, as found for Grid objects, but for a DockPanel.
   /// It works similar to a GridSplitter object.  To use it, put it between 2 other objects on a DockPanel, give it a with or height and set it's
   /// DockPanel.Dock attached property the same as the object before it in the list, which is the object that the splitter 
   /// will control.
   /// </para>
   /// <para>
   /// Since the class descends from a Control, it is templateble.  This means you can change or add functionality to the splitter, for 
   /// instance, you could put a button on it to automatically collaps or open the object being controlled.  The 'BrowserTemplateDemo' 
   /// application shows how you can accomplish this.
   /// </para>
   /// </remarks>
   /// <example>
   /// <code lang="xml"> 
   /// <![CDATA[
   /// <DockPanel>
   /// <TextBox DockPanel.Dock="Bottom"/>
   ///   <t:DockSplitter DockPanel.Dock="Bottom" ></t:DockSplitter>
   ///   <TextBox DockPanel.Dock="Top"/>
   ///   <t:DockSplitter DockPanel.Dock="Top" ></t:DockSplitter>
   ///   <TextBox DockPanel.Dock="Top"/>
   ///   <t:DockSplitter DockPanel.Dock="Top"  ></t:DockSplitter>
   ///   <TextBox DockPanel.Dock="Bottom"/>
   ///   <t:DockSplitter DockPanel.Dock="Bottom"  ></t:DockSplitter>
   ///   <TextBox DockPanel.Dock="Left"/>
   ///   <t:DockSplitter DockPanel.Dock="Left" ></t:DockSplitter>
   ///   <TextBox DockPanel.Dock="Left"/>
   ///   <t:DockSplitter DockPanel.Dock="Left"  ></t:DockSplitter>
   ///   <TextBox DockPanel.Dock="Right"/>
   ///   <t:DockSplitter DockPanel.Dock="Right" ></t:DockSplitter>
   ///   <TextBox DockPanel.Dock="Right"/>
   ///   <t:DockSplitter DockPanel.Dock="Right" ></t:DockSplitter>
   ///   <TextBox DockPanel.Dock="Top"/>
   ///   <t:DockSplitter DockPanel.Dock="Top" ></t:DockSplitter>
   ///   <TextBox/>
   /// </DockPanel>
   /// ]]>
   /// </code>
   /// </example>
   public class DockSplitter : Thumb
   {
      #region fields

      private DockSplitter.ResizeData fResizeData;

      #endregion

      #region ctor

      /// <summary>
      /// Override default values, register event handlers.
      /// </summary>
      static DockSplitter()
      {
         DefaultStyleKeyProperty.OverrideMetadata(typeof(DockSplitter), new FrameworkPropertyMetadata(typeof(DockSplitter)));
         EventManager.RegisterClassHandler(typeof(DockSplitter), Thumb.DragStartedEvent, new DragStartedEventHandler(DockSplitter.OnDragStarted));
         EventManager.RegisterClassHandler(typeof(DockSplitter), Thumb.DragDeltaEvent, new DragDeltaEventHandler(DockSplitter.OnDragDelta));
         EventManager.RegisterClassHandler(typeof(DockSplitter), Thumb.DragCompletedEvent, new DragCompletedEventHandler(DockSplitter.OnDragCompleted));

         UIElement.FocusableProperty.OverrideMetadata(typeof(DockSplitter), new FrameworkPropertyMetadata(true));
         //FrameworkElement.CursorProperty.OverrideMetadata(typeof(DockSplitter), new FrameworkPropertyMetadata(null, new CoerceValueCallback(DockSplitter.CoerceCursor)));

      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      public DockSplitter()
      {
         Loaded += new RoutedEventHandler(DockSplitter_Loaded);
      }


      #endregion

      #region helpers

      void DockSplitter_Loaded(object sender, RoutedEventArgs e)
      {
         Loaded -= new RoutedEventHandler(DockSplitter_Loaded);
         if (ReadLocalValue(ValueProperty) == DependencyProperty.UnsetValue)
            Value = CalculateValue();
      }

      ///// <summary>
      ///// Checks the value for the Cursor property and returns a default value if there is no cursor set.
      ///// </summary>
      ///// <param name="sender">The object you want to check the value for</param>
      ///// <param name="value">The value that is assigned to the property.</param>
      ///// <returns>The cursor to use.  This is either a default WE/NS cursor or the one you assigned.</returns>
      //private static object CoerceCursor(DependencyObject sender, object value)
      //{
      //   Dock iDock;
      //   DockSplitter iSender = sender as DockSplitter;

      //   if (iSender != null)
      //   {
      //      if (value == null)
      //      {
      //         iDock = DockPanel.GetDock(iSender);
      //         switch (iDock)
      //         {
      //            case Dock.Left:
      //            case Dock.Right:
      //               return Cursors.SizeWE;
      //            case Dock.Bottom:
      //            case Dock.Top:
      //               return Cursors.SizeNS;
      //         }
      //      }
      //   }
      //   return value;
      //}

      ///// <summary>
      ///// Called when the rendered size of a control changes.
      ///// </summary>
      ///// <remarks>
      ///// It is possible that we need to adjust the cursor due to the change in size.</remarks>
      ///// <param name="sizeInfo">Specifies the size changes.</param>
      //protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
      //{
      //   base.OnRenderSizeChanged(sizeInfo);
      //   base.CoerceValue(FrameworkElement.CursorProperty);
      //}


      /// <summary>
      /// This is the static wrapper for the OnDragStarted method (WPF way)
      /// </summary>
      private static void OnDragStarted(object sender, DragStartedEventArgs e)
      {
         (sender as DockSplitter).OnDragStarted();
      }


      /// <summary>
      /// Initialises dragging.
      /// </summary>
      private void OnDragStarted()
      {
         DockPanel iDock = base.Parent as DockPanel;
         if (iDock != null)
         {
            int iIndex = FindPreviousDockItemIndex(iDock);
            if (iIndex > -1)
            {
               fResizeData = new DockSplitter.ResizeData();
               fResizeData.Dockpanel = iDock;
               fResizeData.PrevItem = iDock.Children[iIndex] as FrameworkElement;
               if (ResizeMode == DockSplitterResizeMode.Adjacent)
               {
                  fResizeData.NextItem = FindNextVisible(iDock, iIndex + 2, DockPanel.GetDock(this));       //+2 cause iIndex is prev item, iIndex+1 is the splitter, we need to find next, so start looking from next.
               }
               if (fResizeData.NextItem != null && fResizeData.NextItem == FindLastVisible(iDock))          //if the next item is the last item, don't store a ref to it cause we don't want it to resize (the last item must be able to fill the client area.
               {
                  fResizeData.NextItem = null;
               }
               fResizeData.ShowsPreview = ShowsPreview;
               SetResizeDirection();
               SetMinMaxValues();
               SetupPreview();
            }
         }
      }

      #region SetMinMaxValues
      /// <summary>
      /// calculates the bounderies that the splitter must stay in.
      /// </summary>
      private void SetMinMaxValues()
      {
         if (fResizeData.Dockpanel != null && fResizeData.PrevItem != null)
         {
            switch (fResizeData.Dock)
            {
               case Dock.Bottom:
                  SetMinMaxValuesBottom(fResizeData.PrevItem);
                  break;
               case Dock.Left:
                  SetMinMaxValuesLeft(fResizeData.PrevItem);
                  break;
               case Dock.Right:
                  SetMinMaxValuesRight(fResizeData.PrevItem);
                  break;
               case Dock.Top:
                  SetMinMaxValuesTop(fResizeData.PrevItem);
                  break;
               default: throw new NotSupportedException();
            }
         }

      }

      /// <summary>
      /// Algorithm:
      /// - If resizeMode is Box:
      /// we initialize the default maximum resize to the end of the DockPanel.
      /// if there is a control on the dockPanel after the splitter that is on
      /// the oposite side, this becomes the maximum resize. This creates the 
      /// effect that the last object on the dockPanel is resized to compensate
      /// for resize of the control in front of the splitter.
      /// - otherwise, take the size of the object after the splitter.
      /// </summary>
      /// <param name="prev">The object in front of the splitter</param>
      private void SetMinMaxValuesTop(FrameworkElement prev)
      {
         if (fResizeData.NextItem != null)
            fResizeData.MaxValue = fResizeData.NextItem.ActualHeight;
         else
         {
            Point iBottom = TranslatePoint(new Point(ActualWidth, ActualHeight), fResizeData.Dockpanel);
            FrameworkElement iEl = FindLastVisible(fResizeData.Dockpanel);
            if (iEl != null)
            {
               Point iTop = iEl.TranslatePoint(new Point(iEl.ActualWidth, iEl.ActualHeight), fResizeData.Dockpanel);
               fResizeData.MaxValue = iTop.Y - iBottom.Y;
            }
            else
               fResizeData.MaxValue = fResizeData.Dockpanel.ActualHeight - iBottom.Y;

         }
         fResizeData.MinValue = -prev.ActualHeight;
      }

      /// <summary>
      /// see <see cref="DockSplitter.SetMinMaxValuesTop"/>
      /// </summary>
      private void SetMinMaxValuesRight(FrameworkElement prev)
      {
         if (fResizeData.NextItem != null)
            fResizeData.MinValue = -fResizeData.NextItem.ActualWidth;
         else
         {
            Point iTop = TranslatePoint(new Point(0, 0), fResizeData.Dockpanel);
            FrameworkElement iEl = FindLastVisible(fResizeData.Dockpanel);
            if (iEl != null)
            {
               Point iBottom = iEl.TranslatePoint(new Point(0, 0), fResizeData.Dockpanel);
               fResizeData.MinValue = -iTop.X + iBottom.X;
            }
            else
               fResizeData.MinValue = -iTop.X;
         }
         fResizeData.MaxValue = prev.ActualWidth;
      }

      /// <summary>
      /// see <see cref="DockSplitter.SetMinMaxValuesTop"/>
      /// </summary>
      private void SetMinMaxValuesLeft(FrameworkElement prev)
      {
         if (fResizeData.NextItem != null)
            fResizeData.MaxValue = fResizeData.NextItem.ActualWidth;
         else
         {
            FrameworkElement iEl = FindLastVisible(fResizeData.Dockpanel);
            if (iEl != null)
            {
               Point iTop = TranslatePoint(new Point(0, 0), fResizeData.Dockpanel);
               Point iBottom = iEl.TranslatePoint(new Point(iEl.ActualWidth, iEl.ActualHeight), fResizeData.Dockpanel);
               fResizeData.MaxValue = iBottom.X - iTop.X;
            }
            else
            {
               Point iBottom = TranslatePoint(new Point(ActualWidth, ActualHeight), fResizeData.Dockpanel);
               fResizeData.MaxValue = fResizeData.Dockpanel.ActualWidth - iBottom.X;
            }
         }
         fResizeData.MinValue = -prev.ActualWidth;
      }

      /// <summary>
      /// calculates the Min Max values for a Splitter that is docked to the bottom.
      /// </summary>
      private void SetMinMaxValuesBottom(FrameworkElement prev)
      {
         if (fResizeData.NextItem != null)
            fResizeData.MinValue = -fResizeData.NextItem.ActualHeight;
         else
         {
            FrameworkElement iEl = FindLastVisible(fResizeData.Dockpanel);
            Point iTop = TranslatePoint(new Point(0, 0), fResizeData.Dockpanel); ;
            if (iEl != null)
            {
               Point iTop2 = iEl.TranslatePoint(new Point(0, 0), fResizeData.Dockpanel);
               fResizeData.MinValue = iTop2.Y - iTop.Y;
            }
            else
               fResizeData.MinValue = -iTop.Y;
         }
         fResizeData.MaxValue = prev.ActualHeight;
      } 
      #endregion

      /// <summary>
      /// Calculates the direction we want to resize in.  This depends on The Dock value.
      /// </summary>
      /// <remarks>
      /// If Dock == Left or Right, Resize direction is Columns, otherwise it's Rows.
      /// </remarks>
      private void SetResizeDirection()
      {
         Dock iDock = DockPanel.GetDock(this);

         if (iDock == Dock.Left || iDock == Dock.Right)
            this.fResizeData.ResizeDirection = GridResizeDirection.Columns;
         else
            this.fResizeData.ResizeDirection = GridResizeDirection.Rows;
         this.fResizeData.Dock = iDock;
      }

      /// <summary>
      /// Finds the last FrameworkElement on the DockPanel and returns it.
      /// </summary>
      /// <param name="panel">The panel to search on</param>
      /// <returns>The object found or null.</returns>
      private static FrameworkElement FindLastVisible(DockPanel panel)
      {
         for (int i = panel.Children.Count - 1; i >= 0; i--)
         {
            FrameworkElement iEl = panel.Children[i] as FrameworkElement;
            if (iEl != null && iEl.Visibility == Visibility.Visible)
            {
               return iEl as FrameworkElement;
            }
         }
         return null;
      }

      /// <summary>
      /// Finds the next visible FrameworkElement with the specified Dock value starting at the specified index.
      /// </summary>
      /// <remarks>
      /// This property is used to find the 'percieved' next object.  This is found using the following algorithm:
      /// -walk through all the items from the index, if:
      ///   -an item is found with the same dock value, return this, cause that is the closest.
      /// - if no items was found with the same dock value, return the last item cause this serves as the filler, which can always be resized.
      /// </remarks>
      /// <param name="panel">The panel to search on.</param>
      /// <param name="index">The index to start searching from.</param>
      /// <param name="dock">The Dock value to search for.</param>
      /// <returns>The object found or null.</returns>
      private static FrameworkElement FindNextVisible(DockPanel panel, int index, Dock dock)
      {
         if (index > -1)
         {  
            for (int i = index; i < panel.Children.Count; i++)
            {
               FrameworkElement iEl = panel.Children[i] as FrameworkElement;
               if (iEl != null && iEl.Visibility == Visibility.Visible)
               {
                  Dock iElDock = DockPanel.GetDock(iEl);
                  if (iElDock == dock)
                  {
                     return iEl as FrameworkElement;
                  }
               }
            }
            return panel.Children[panel.Children.Count - 1] as FrameworkElement;           //note; we don't check if aPanel actually has children, this is presumed if aIndex > -1
         }
         return null;
      }

      /// <summary>
      /// Looks for the next visible item on the DockPanel.
      /// </summary>
      /// <param name="panel">The DockPanel to search on.</param>
      /// <returns>a reference to the next visible item.</returns>
      private UIElement FindNextDockItem(DockPanel panel)
      {
         if (panel != null)                                          //could be that we werent put on a DockPanel.
         {
            int iIndex = panel.Children.IndexOf(this);
            for (int i = iIndex + 1; i < panel.Children.Count; i++)
            {
               if (panel.Children[i].Visibility == Visibility.Visible)
               {
                  return panel.Children[i];
               }
            }
         }
         return null;                                                //if we get here, haven't found a previous item.
      }

      /// <summary>
      /// look for the previous item in the dock panel.
      /// </summary>
      /// <remarks>
      /// This function returns an index instead of the item itself. This is used when the drag begins to allow fast walk through all the previous
      /// items.
      /// </remarks>
      /// <param name="panel">The panel to search on.</param>
      /// <returns>The index of the previous item</returns>
      private int FindPreviousDockItemIndex(DockPanel panel)
      {
         if (panel != null)                                          //could be that we werent put on a DockPanel.
         {
            int iIndex = panel.Children.IndexOf(this);
            for (int i = iIndex - 1; i >= 0; i--)
            {
               if (panel.Children[i].Visibility == Visibility.Visible)
               {
                  return i;
               }
            }
         }
         return -1;                                                //if we get here, haven't found a previous item.
      }


      /// <summary>
      /// Returns the previous object.
      /// </summary>
      /// <remarks>
      /// This function is similar to <see cref="DockSplitter.FindPreviousDockItemIndex"/> which returns the index number.
      /// </remarks>
      /// <param name="panel">The DockPanel to search on.</param>
      /// <returns>The first object before the splitter that is visible or null.</returns>
      private UIElement FindPreviousDockItem(DockPanel panel)
      {
         if (panel != null)                                          //could be that we werent put on a DockPanel.
         {
            int iIndex = panel.Children.IndexOf(this);
            for (int i = iIndex - 1; i >= 0; i--)
            {
               if (panel.Children[i].Visibility == Visibility.Visible)
               {
                  return panel.Children[i];
               }
            }
         }
         return null;                                                //if we get here, haven't found a previous item.
      }


      /// <summary>
      /// creates an Adorner layer and puts the control on that shows the drag location.
      /// </summary>
      private void SetupPreview()
      {
         if (fResizeData.ShowsPreview == true)
         {
            AdornerLayer iLayer = AdornerLayer.GetAdornerLayer(fResizeData.Dockpanel);
            if (iLayer != null)
            {
               fResizeData.Adorner = new DockSplitter.PreviewAdorner(this, PreviewStyle);
               iLayer.Add(this.fResizeData.Adorner);
            }
         }
      }

      /// <summary>
      /// Static wrapper for OnDragDelta
      /// </summary>
      private static void OnDragDelta(object sender, DragDeltaEventArgs e)
      {
         (sender as DockSplitter).OnDragDelta(e);
      }


      /// <summary>
      /// Called whenever the splitters is dragged around.
      /// </summary>
      /// <param name="e">Event arguments, contains the amount of movement.</param>
      private void OnDragDelta(DragDeltaEventArgs e)
      {
         if (fResizeData != null)
         {
            double iHor = e.HorizontalChange;
            double iVer = e.VerticalChange;
            double iIncrement = DragIncrement;
            iHor = Math.Round((double)(iHor / iIncrement)) * iIncrement;
            iVer = Math.Round((double)(iVer / iIncrement)) * iIncrement;
            if (fResizeData.ShowsPreview == true)
            {
               double iVal;
               if (fResizeData.ResizeDirection == GridResizeDirection.Columns)
               {
                  iVal = Math.Min(Math.Max(iHor, fResizeData.MinValue), fResizeData.MaxValue);                       //keep within bounderies
                  fResizeData.Adorner.OffsetX = iVal;
               }
               else
               {
                  iVal = Math.Min(Math.Max(iVer, this.fResizeData.MinValue), this.fResizeData.MaxValue);             //keep within bounderies
                  fResizeData.Adorner.OffsetY = iVal;
               }
            }
            else
            {
               if (fResizeData.ResizeDirection == GridResizeDirection.Columns)
               {
                  iHor = Math.Min(Math.Max(iHor, fResizeData.MinValue), fResizeData.MaxValue);                          //we still need to make certain that we don't resize outside of the bounderies.
                  fResizeData.MinValue -= iHor;
                  fResizeData.MaxValue -= iHor;
               }
               else
               {
                  iVer = Math.Min(Math.Max(iVer, this.fResizeData.MinValue), this.fResizeData.MaxValue);
                  fResizeData.MaxValue -= iVer;
                  fResizeData.MinValue -= iVer;
                  Debug.Print(string.Format("Value: {0}, Min: {1}, Max: {2}", iVer, fResizeData.MinValue, fResizeData.MaxValue));
               }
               MoveSplitter(iHor, iVer);
            }
         }
      }

      /// <summary>
      /// Moves the actual splitter object with the specified offset values.
      /// </summary>
      /// <param name="horChange">The amount of horizontal change.</param>
      /// <param name="verChange">The amount of vertical change.</param>
      private void MoveSplitter(double horChange, double verChange)
      {
         FrameworkElement iPrev = fResizeData.PrevItem;
         if (iPrev != null)
         {
            switch (fResizeData.Dock)
            {
               case Dock.Bottom:
                  Value = iPrev.ActualHeight - verChange;
                  break;
               case Dock.Left:
                  Value = iPrev.ActualWidth + horChange;
                  break;
               case Dock.Right:
                  Value = iPrev.ActualWidth - horChange;
                  break;
               case Dock.Top:
                  Value = iPrev.ActualHeight + verChange;
                  break;
               default: throw new NotSupportedException();
            }
         }
      }

      private static void OnDragCompleted(object sender, DragCompletedEventArgs e)
      {
         DockSplitter iSender = sender as DockSplitter;
         if (iSender != null)
         {
            iSender.OnDragCompleted();
         }
      }

      /// <summary>
      /// called when dragging is terminated.  Removes the preview info and moves the splitter if needed.
      /// </summary>
      private void OnDragCompleted()
      {
         if (fResizeData != null)
         {
            if (fResizeData.ShowsPreview)
            {
               MoveSplitter(fResizeData.Adorner.OffsetX, fResizeData.Adorner.OffsetY);
               RemovePreviewAdorner();
            }
            fResizeData = null;
         }
      }

      /// <summary>
      /// Removes the object that represents the new position (during preview) from the adorner layer when draggind is done.
      /// </summary>
      private void RemovePreviewAdorner()
      {
         if (fResizeData.Adorner != null)
         {
            (VisualTreeHelper.GetParent(this.fResizeData.Adorner) as AdornerLayer).Remove(fResizeData.Adorner);
         }
      }

      /// <summary>
      /// checks if the delta value (of the Thumb) is valid.
      /// </summary>
      private static bool IsValidDelta(object o)
      {
         double iNum = (double)o;
         if (iNum > 0)
         {
            return !double.IsPositiveInfinity(iNum);
         }
         return false;
      }

      //moet public weg halen, momenteel nog nodig voor DockingControl.
      /// <summary>
      /// Assigns the value Height/Width property to the object being controlled without saving the value.
      /// </summary>
      /// <param name="val">The value to assign to the object being controlled.</param>
      public void ApplyValue(double val)
      {
          DockPanel iDockPanel = WpfTreeHelper.FindInTree<DockPanel>(this) as DockPanel;
         if (iDockPanel != null)
         {
            Dock iDock = DockPanel.GetDock(this);
            FrameworkElement iPrev = null;
            FrameworkElement iNext = null;
            if (fResizeData != null)
            {
               iPrev = fResizeData.PrevItem;
               iNext = fResizeData.NextItem;
            }
            else
            {
               int iIndex = FindPreviousDockItemIndex(iDockPanel);
               if (iIndex > -1)
               {
                  iPrev = iDockPanel.Children[iIndex] as FrameworkElement;
                  iNext = FindNextVisible(iDockPanel, iIndex + 2, iDock);                                //+2 cause iIndex is prev item, iIndex+1 is the splitter, we need to find next, so start looking from next.
                  if (iNext == FindLastVisible(iDockPanel))                                              //the last visible item on the list doesn't have to be resized, otherwise the 'LastChildFil' breaks
                     iNext = null;
               }
            }

            if (iPrev != null && val >= 0.0)
            {
               switch (iDock)
               {
                  case Dock.Bottom:
                     if (iNext != null && iNext.IsLoaded == true)                                           //we need to check for IsLoaded here, otherwise ActualXXx returns 0.0, which is usually incorrect -> XXX gets incorrect value, when loaded, this is correct value cause all is set.
                        iNext.Height = Math.Max(iNext.ActualHeight + iPrev.ActualHeight - val, 0);          //to compensate for rounding errors.s
                     iPrev.Height = val;                                                                    //Don't kneed to check for IsLoaded, we assign a static value to XXX
                     break;
                  case Dock.Left:
                     if (iNext != null && iNext.IsLoaded == true)                                           //we need to check for IsLoaded here, otherwise ActualXXx returns 0.0, which is usually incorrect -> XXX gets incorrect value, when loaded, this is correct value cause all is set.
                        iNext.Width = Math.Max(iNext.ActualWidth + iPrev.ActualWidth - val, 0);             //to compensate for rounding errors.
                     iPrev.Width = val;
                     break;
                  case Dock.Right:
                     if (iNext != null && iNext.IsLoaded == true)                                           //we need to check for IsLoaded here, otherwise ActualXXx returns 0.0, which is usually incorrect -> XXX gets incorrect value, when loaded, this is correct value cause all is set.
                        iNext.Width = Math.Max(iNext.ActualWidth + iPrev.ActualWidth - val, 0);             //to compensate for rounding errors.
                     iPrev.Width = val;
                     break;
                  case Dock.Top:
                     if (iNext != null && iNext.IsLoaded == true)                                           //we need to check for IsLoaded here, otherwise ActualXXx returns 0.0, which is usually incorrect -> XXX gets incorrect value, when loaded, this is correct value cause all is set.
                        iNext.Height = Math.Max(iNext.ActualHeight - val + iPrev.ActualHeight, 0);           //to compensate for rounding errors.
                     iPrev.Height = val;
                     break;
                  default: throw new NotSupportedException();
               }
            }
         }
      }

      #endregion

      #region prop

      /// <summary>
      /// Provides a quick reference to the object that is being resized.
      /// </summary>
      /// <remarks>
      /// This property returns the first item before the splitter on the DockPanel that is visible.  This is the object being resized.
      /// </remarks>
      public UIElement ResizedObject
      {
         get
         {
            DockPanel iDockPanel = Parent as DockPanel;
            if (iDockPanel != null)
            {
               return FindPreviousDockItem(iDockPanel);
            }
            return null;
         }
      }

      /// <summary>
      /// Identifies the Size dependency property.
      /// </summary>
      public static readonly DependencyProperty SizeProperty = DependencyProperty.Register("Size", typeof(double), typeof(DockSplitter),
         new FrameworkPropertyMetadata(6.0));


      /// <summary>
      /// Gets/sets the Width/Height of the control.
      /// </summary>
      /// <remarks>
      /// <para>
      /// Either the Height or Width of the control is determined by the DockPanel.Dock value assigned to the control while placed in 
      /// a DockPanel.  This property determins the value of the property not controlled by the DockPanel.
      /// </para>
      /// <para>
      /// This property provides an easy, transparent way of changing the Height/Width of the control without having to know the 
      /// DockPanel.Dock value.  This is primarely used in styles so you can define 1 value for either Height or Width.
      /// </para>
      /// </remarks>
      /// <seealso cref="DockSplitter"/>
      /// <seealso cref="DockSplitter.Value"/>
      public double Size
      {
         get
         {
            return (double)base.GetValue(SizeProperty);
         }
         set
         {
            base.SetValue(SizeProperty, value);
         }
      }

      /// <summary>
      /// Identifies the Value dependency property.
      /// </summary>
      public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(double), typeof(DockSplitter),
         new FrameworkPropertyMetadata(double.NaN, new PropertyChangedCallback(OnValueChanged)));
      /// <summary>
      /// Gets/sets the current value of the DockSplitter.  This is a dependency property.
      /// </summary>
      /// <remarks>
      /// <para>
      /// Use this property if you want to assign a new position to the DockSplitter from code.
      /// </para>
      /// <para>
      /// The initial value is retrieved from the object being controlled . 
      /// </para>
      /// </remarks>
      /// <example>
      /// The following example demonstrates how you can collaps/expand a DockSplitter by placing
      /// a button on it through a template.
      /// The code collapses the associated object by setting it's with/height to 0.0, or restoring it to it's default value stored in the Tag property.
      /// Collapsing and expanding is done through an animation, there is also a non animation form written as comment where the new value is simply assigned to the property.
      /// <code>
      /// void OnClickSplit(object aSender, EventArgs e)
      /// {
      ///    Button iOrigin = aSender as Button;
      ///    if (iOrigin != null)
      ///    {
      ///       DockSplitter iSender = iOrigin.TemplatedParent as DockSplitter;
      ///       if (iSender != null)
      ///       {
      ///          if (iSender.Value == 0.0)
      ///          {
      ///             DoubleAnimation iValueAnimation = new DoubleAnimation((double)iSender.Tag, new TimeSpan(0, 0, 0, 0, 70));
      ///             AnimationClock iMyControllableClock = iValueAnimation.CreateClock();
      ///             iSender.ApplyAnimationClock(DockSplitter.ValueProperty, iMyControllableClock);
      ///             //iSender.Value = (double)iSender.Tag;
      ///          }
      ///          else
      ///          {
      ///             iSender.Tag = iSender.Value;
      ///             DoubleAnimation iValueAnimation = new DoubleAnimation(0.0, new TimeSpan(0, 0, 0, 0, 120));
      ///             AnimationClock iMyControllableClock = iValueAnimation.CreateClock();
      ///             iSender.ApplyAnimationClock(DockSplitter.ValueProperty, iMyControllableClock);
      ///             //iSender.Value = 0.0;
      ///          }
      ///       }
      ///    }
      /// }
      /// </code>
      /// This is the xaml to define the button.
      /// <code lang="xml">
      /// <![CDATA[
      /// <Style TargetType={x:Type d:DockSplitter} >
      ///   <Setter Property="d:DockSplitter.Template">
      ///      <Setter.Value>
      ///         <ControlTemplate>
      ///            <Border Background="{TemplateBinding Background}">
      ///               <DockPanel LastChildFill="False"
      ///                          HorizontalAlignment="Center"
      ///                          VerticalAlignment="Center"
      ///                          >
      ///                  <Button x:Name="CloseSplit"
      ///                          Click="OnClickSplit"
      ///                          Focusable="True"
      ///                          Cursor="Arrow">
      ///                     <Image Source ="{StaticResource SplitterCloseImage}"/>
      ///                     <Button.Template>
      ///                        <ControlTemplate>
      ///                           <ContentPresenter Content ="{TemplateBinding Button.Content}"/>
      ///                        </ControlTemplate>
      ///                     </Button.Template>
      ///                  </Button>
      ///               </DockPanel>
      ///            </Border>
      /// 
      ///            <ControlTemplate.Triggers>
      ///               <Trigger Property="DockPanel.Dock" Value="Top">
      ///                  <Setter Property="Control.Height" Value="{Binding Path=Size, RelativeSource={RelativeSource Self}}"/>
      ///                  <Setter Property="Control.Cursor" Value="SizeNS"/>
      ///                  <Setter TargetName="CloseSplit" Property="Button.LayoutTransform">
      ///                     <Setter.Value>
      ///                        <RotateTransform Angle="90"/>
      ///                     </Setter.Value>
      ///                  </Setter>
      ///               </Trigger>
      ///               <MultiTrigger>
      ///                  <MultiTrigger.Conditions>
      ///                     <Condition Property="DockPanel.Dock" Value="Top"/>
      ///                     <Condition Property="d:DockSplitter.Value" Value="0.0"/>
      ///                  </MultiTrigger.Conditions>
      ///                  <Setter Property="Control.Height" Value="{Binding Path=Size, RelativeSource={RelativeSource Self}}"/>
      ///                  <Setter Property="Control.Cursor" Value="SizeNS"/>
      ///                  <Setter TargetName="CloseSplit" Property="Button.LayoutTransform">
      ///                     <Setter.Value>
      ///                        <RotateTransform Angle="-90"/>
      ///                     </Setter.Value>
      ///                  </Setter>
      ///               </MultiTrigger>
      ///               <Trigger Property="DockPanel.Dock" Value="Bottom">
      ///                  <Setter Property="Control.Height" Value="{Binding Path=Size, RelativeSource={RelativeSource Self}}"/>
      ///                  <Setter Property="Control.Cursor" Value="SizeNS"/>
      ///                  <Setter TargetName="CloseSplit" Property="Button.LayoutTransform">
      ///                     <Setter.Value>
      ///                        <RotateTransform Angle="-90"/>
      ///                     </Setter.Value>
      ///                  </Setter>
      ///               </Trigger>
      ///               <MultiTrigger>
      ///                  <MultiTrigger.Conditions>
      ///                     <Condition Property="DockPanel.Dock" Value="Bottom"/>
      ///                     <Condition Property="d:DockSplitter.Value" Value="0.0"/>
      ///                  </MultiTrigger.Conditions>
      ///                  <Setter Property="Control.Height" Value="{Binding Path=Size, RelativeSource={RelativeSource Self}}"/>
      ///                  <Setter Property="Control.Cursor" Value="SizeNS"/>
      ///                  <Setter TargetName="CloseSplit" Property="Button.LayoutTransform">
      ///                     <Setter.Value>
      ///                        <RotateTransform Angle="90"/>
      ///                     </Setter.Value>
      ///                  </Setter>
      ///               </MultiTrigger>
      ///               <Trigger Property="DockPanel.Dock" Value="Right">
      ///                  <Setter Property="Control.Width" Value="{Binding Path=Size, RelativeSource={RelativeSource Self}}"/>
      ///                  <Setter Property="Control.Cursor" Value="SizeWE"/>
      ///                  <Setter TargetName="CloseSplit" Property="Button.LayoutTransform">
      ///                     <Setter.Value>
      ///                        <RotateTransform Angle="180"/>
      ///                     </Setter.Value>
      ///                  </Setter>
      ///               </Trigger>
      ///               <MultiTrigger>
      ///                  <MultiTrigger.Conditions>
      ///                     <Condition Property="DockPanel.Dock" Value="Right"/>
      ///                     <Condition Property="d:DockSplitter.Value" Value="0.0"/>
      ///                  </MultiTrigger.Conditions>
      ///                  <Setter Property="Control.Width" Value="{Binding Path=Size, RelativeSource={RelativeSource Self}}"/>
      ///                  <Setter Property="Control.Cursor" Value="SizeWE"/>
      ///                  <Setter TargetName="CloseSplit" Property="Button.LayoutTransform" Value="{x:Null}"/>
      ///               </MultiTrigger>
      ///               <Trigger Property="DockPanel.Dock" Value="Left">
      ///                  <Setter Property="Control.Width" Value="{Binding Path=Size, RelativeSource={RelativeSource Self}}"/>
      ///                  <Setter Property="Control.Cursor" Value="SizeWE"/>
      ///               </Trigger>
      ///               <MultiTrigger>
      ///                  <MultiTrigger.Conditions>
      ///                     <Condition Property="DockPanel.Dock" Value="Left"/>
      ///                     <Condition Property="d:DockSplitter.Value" Value="0.0"/>
      ///                  </MultiTrigger.Conditions>
      ///                  <Setter Property="Control.Width" Value="{Binding Path=Size, RelativeSource={RelativeSource Self}}"/>
      ///                  <Setter Property="Control.Cursor" Value="SizeWE"/>
      ///                  <Setter TargetName="CloseSplit" Property="Button.LayoutTransform">
      ///                     <Setter.Value>
      ///                        <RotateTransform Angle="180"/>
      ///                     </Setter.Value>
      ///                  </Setter>
      ///               </MultiTrigger>             
      ///            </ControlTemplate.Triggers>
      ///         </ControlTemplate>
      ///      </Setter.Value>
      ///   </Setter>
      /// </Style>
      /// ]]>
      /// </code>
      /// </example>
      /// <seealso cref="DockSplitter"/>
      public double Value
      {
         get
         {
            return (double)base.GetValue(DockSplitter.ValueProperty);
         }
         set
         {
            base.SetValue(DockSplitter.ValueProperty, value);
         }
      }

      private double CalculateValue()
      {
         DockPanel iDockPanel = Parent as DockPanel;
         if (iDockPanel != null)
         {
            FrameworkElement iPrev = FindPreviousDockItem(iDockPanel) as FrameworkElement;
            if (iPrev != null)
            {
               Dock iDock = DockPanel.GetDock(this);
               switch (iDock)
               {
                  case Dock.Bottom: return iPrev.ActualHeight;
                  case Dock.Left: return iPrev.ActualWidth;
                  case Dock.Right: return iPrev.ActualWidth;
                  case Dock.Top: return iPrev.ActualHeight;
                  default: throw new NotSupportedException();
               }
            }
         }
         return double.NaN;
      }

      static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
      {
         DockSplitter iSender = sender as DockSplitter;

         if (iSender != null)
         {
            iSender.ApplyValue((double)e.NewValue);
         }
      }

      /// <summary>
      /// Identifies the DragIncrement dependency property.
      /// </summary>
      public static readonly DependencyProperty DragIncrementProperty = DependencyProperty.Register("DragIncrement", typeof(double), typeof(DockSplitter), new FrameworkPropertyMetadata(1.0), new ValidateValueCallback(DockSplitter.IsValidDelta));
      /// <summary>
      /// Gets/sets the minimum distance that a user must drag a mouse to resize the object being controlled with a DockSplitter.  This is a dependency property.
      /// </summary>
      /// <seealso cref="DockSplitter"/>
      public double DragIncrement
      {
         get
         {
            return (double)base.GetValue(DockSplitter.DragIncrementProperty);
         }
         set
         {
            base.SetValue(DockSplitter.DragIncrementProperty, value);
         }
      }

      /// <summary>
      /// Identifies the ResizeMode dependency property.
      /// </summary>
      public static readonly DependencyProperty ResizeModeProperty = DependencyProperty.Register("ResizeMode", typeof(DockSplitterResizeMode), typeof(DockSplitter), new FrameworkPropertyMetadata(DockSplitterResizeMode.Adjacent));
      /// <summary>
      /// Gets/sets how a DockSplitter resizes the objects on a DockPanel.  This is a dependency property.
      /// </summary>
      /// <remarks>
      /// <para>
      /// By default, only the object in front of the splitter (the first visible object) is resized.  This usually means that all objects after the splitter 
      /// are moved and if the DockPanel is set up to resize the last object to fill the client area, this object will also be resized.  Sometimes this isn't 
      /// the desired behaviour but instead the object after the splitter should become bigger or shrink agording to the size change of the object before the 
      /// splitter so that the position and size of all the other objects remains the same.  This can be achieved by setting this property to 'Adjacent'.
      /// </para>
      /// <para>
      /// Changing this property also has an effect on how far the resize operation can go.  By default, resizing can be done in a 'box' like structure. This
      /// means that the last control on the DockPanel that has a DockValue opposite to that of the splitter determins the maximum resize value (creating a
      /// box effect: the last object on the DockPanel is resized).  If you change this property to 'Adjacent', the maximum size is limited to the 
      /// size of the next control.  The minimum size is always the size of the control in front of the splitter.
      /// set this property
      /// </para>
      /// </remarks>
      /// <seealso cref="DockSplitter"/>
      public DockSplitterResizeMode ResizeMode
      {
         get
         {
            return (DockSplitterResizeMode)base.GetValue(ResizeModeProperty);
         }
         set
         {
            base.SetValue(ResizeModeProperty, value);
         }
      }



      /// <summary>
      /// Identifies the PreviewStyle dependency property.
      /// </summary>
      public static readonly DependencyProperty PreviewStyleProperty = DependencyProperty.Register("PreviewStyle", typeof(Style), typeof(DockSplitter), new FrameworkPropertyMetadata(null));
      /// <summary>
      /// Getts/sets the style that customizes the appearance, effects, or other style characteristics for the DockSplitter control preview indicator that is displayed when the ShowsPreview property is set to true.  This is a dependency property.
      /// </summary> 
      /// <remarks>
      /// Assign a style to this property if you want to change the default look of the preview style.  Usually this is done in xaml.
      /// </remarks>
      /// <example>
      /// <code lang="xml">
      /// <![CDATA[
      /// <Style x:Key="{x:Type d:DockSplitter}">
      ///   <Setter Property="Control.Background" Value="#FF2E2E2E"/>
      ///   <Setter Property="d:DockSplitter.PreviewStyle">
      ///      <Setter.Value>
      ///         <Style>
      ///            <Setter Property="Control.Template">
      ///               <Setter.Value>
      ///                  <ControlTemplate>
      ///                     <Border Background="Azure"/>
      ///                  </ControlTemplate>
      ///               </Setter.Value>
      ///            </Setter>
      ///         </Style>
      ///      </Setter.Value>
      ///   </Setter>
      /// </Style>
      /// ]]>
      /// </code>
      /// </example>
      /// <seealso cref="DockSplitter"/>
      /// <seealso cref="DockSplitter.ShowsPreview"/>
      public Style PreviewStyle
      {
         get
         {
            return (Style)base.GetValue(DockSplitter.PreviewStyleProperty);
         }
         set
         {
            base.SetValue(DockSplitter.PreviewStyleProperty, value);
         }
      }


      /// <summary>
      /// Identifies the ShowsPreview dependency property.
      /// </summary>
      public static readonly DependencyProperty ShowsPreviewProperty = DependencyProperty.Register("ShowsPreview", typeof(bool), typeof(DockSplitter), new FrameworkPropertyMetadata(true));
      /// <summary>
      /// Gets/sets whether the DockSplitter control shows a preview or updates the object being controlled as the user drags the control.
      /// </summary>
      /// <remarks>
      /// <para>
      /// By default, this property is 'True' indicating that a preview is shown before the object is resized.  The resizing will only be done after the drag operation.
      /// </para>
      /// <para>
      /// You can control the look of the preview through the <see cref="DockSplitter.PreviewStyle"/> property.
      /// </para>
      /// </remarks>
      /// /// <seealso cref="DockSplitter"/>
      /// /// <seealso cref="DockSplitter.PreviewStyle"/>
      public bool ShowsPreview
      {
         get
         {
            return (bool)base.GetValue(DockSplitter.ShowsPreviewProperty);
         }
         set
         {
            base.SetValue(DockSplitter.ShowsPreviewProperty, value);
         }
      }

      #endregion


      #region inner classes

      private class ResizeData
      {
         // Fields
         public DockSplitter.PreviewAdorner Adorner;
         public DockPanel Dockpanel;
         public double MaxValue;
         public double MinValue;
         public GridResizeDirection ResizeDirection;
         public bool ShowsPreview;
         public Dock Dock;
         public FrameworkElement PrevItem;
         public FrameworkElement NextItem;
      }

      private sealed class PreviewAdorner : Adorner
      {
         // Methods
         public PreviewAdorner(DockSplitter dock, Style previewStyle)
            : base(dock)
         {
            Control iControl = new Button();
            iControl.Style = previewStyle;
            iControl.IsEnabled = false;
            this.Translation = new TranslateTransform();
            iDecorator = new Decorator();
            iDecorator.Child = iControl;
            iDecorator.RenderTransform = this.Translation;
            base.AddVisualChild(iDecorator);
         }



         protected override Size ArrangeOverride(Size finalSize)
         {
            iDecorator.Arrange(new Rect(new Point(), finalSize));
            return finalSize;
         }

         protected override Visual GetVisualChild(int index)
         {
            if (index != 0)
            {
               throw new ArgumentOutOfRangeException("index");
            }
            return iDecorator;
         }

         // Properties
         public double OffsetX
         {
            get
            {
               return this.Translation.X;
            }
            set
            {
               this.Translation.X = value;
            }
         }

         public double OffsetY
         {
            get
            {
               return this.Translation.Y;
            }
            set
            {
               this.Translation.Y = value;
            }
         }

         protected override int VisualChildrenCount
         {
            get
            {
               return 1;
            }

         }

         // Fields
         private Decorator iDecorator;
         private TranslateTransform Translation;
      }

      #endregion

   }
}


