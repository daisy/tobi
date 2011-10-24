using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;

namespace Tobi.Common.UI
{
    //[StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(MenuItemRichCommand))]
    public class MenuItemRichCommand : MenuItem
    {
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            if (item is RichDelegateCommand)
            {
                ConfigureMenuItemFromCommand((MenuItemRichCommand)element, (RichDelegateCommand)item);
            }
            else if (item is TwoStateMenuItemRichCommand_DataContextWrapper)
            {
                var data = (TwoStateMenuItemRichCommand_DataContextWrapper)item;
                var menuItem = (TwoStateMenuItemRichCommand)element;

                menuItem.RichCommandOne = data.RichCommandOne;
                menuItem.RichCommandTwo = data.RichCommandTwo;
                menuItem.InputBindingManager = data.InputBindingManager;

                var binding = new Binding
                {
                    Mode = BindingMode.OneWay,
                    Source = data.RichCommandActive_BindingSource,
                    Path = new PropertyPath(PropertyChangedNotifyBase.GetMemberName(data.RichCommandActive_BindingPropertyPathLambdaExpr))
                };

                var expr = menuItem.SetBinding(TwoStateMenuItemRichCommand.RichCommandActiveProperty, binding);

                // NOT needed because OnRichCommandActiveChanged() is triggered by the above binding statement
                //TwoStateMenuItemRichCommand.ConfigureTwoStateMenuItemRichCommand(menuItem, data.RichCommandActive);
            }

            base.PrepareContainerForItemOverride(element, item);
        }

        private object m_Item;
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            bool own = item is RichDelegateCommand
                || item is MenuItemRichCommand
                || item is Separator
                || item is TwoStateMenuItemRichCommand_DataContextWrapper;
            m_Item = (own ? item : null);
            return false;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            if (m_Item == null)
            {
                return new MenuItem();
            }
            if (m_Item is MenuItemRichCommand)
            {
                return (MenuItemRichCommand)m_Item;
            }
            if (m_Item is RichDelegateCommand)
            {
                var container = new MenuItemRichCommand();
                //ConfigureMenuItemFromCommand(container, (RichDelegateCommand)m_Item);
                return container;
            }
            if (m_Item is TwoStateMenuItemRichCommand_DataContextWrapper)
            {
                var container = new TwoStateMenuItemRichCommand();
                //TwoStateMenuItemRichCommand.ConfigureTwoStateMenuItemRichCommand(container, ((TwoStateMenuItemRichCommand_DataContextWrapper)m_Item).RichCommandActive);
                return container;
            }
            return (DependencyObject)m_Item;
        }

        public static readonly DependencyProperty RichCommandProperty =
            DependencyProperty.Register("RichCommand",
                                        typeof(RichDelegateCommand),
                                        typeof(MenuItemRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandChanged)));

        private static void OnRichCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var menuItem = d as MenuItem;
            if (menuItem == null)
            {
                return;
            }
            var command = e.NewValue as RichDelegateCommand;
            if (command == null)
            {
                return;
            }

            ConfigureMenuItemFromCommand(menuItem, command);
        }

        //protected override bool IsEnabledCore
        //{
        //    get
        //    {
        //        return true;
        //    }
        //}

        public RichDelegateCommand RichCommand
        {
            get
            {
                return (RichDelegateCommand)GetValue(RichCommandProperty);
            }
            set
            {
                SetValue(RichCommandProperty, value);
            }
        }


        public static void SetRichCommand(RichDelegateCommand command, MenuItemRichCommand menuitem, EventHandler dataChangedEventCallback)
        {
            if (menuitem.Command == command)
                return;

            if (menuitem.Command != null
                && menuitem.Command is RichDelegateCommand
                && ((RichDelegateCommand)menuitem.Command).DataChangedHasHandlers)
            {
                ((RichDelegateCommand)menuitem.Command).DataChanged -= dataChangedEventCallback;
            }

            menuitem.Command = command;

            RefreshMenuItemFromItsRichCommand(menuitem);

            command.DataChanged += dataChangedEventCallback;
        }

        public void SetRichCommand(RichDelegateCommand command)
        {
            SetRichCommand(command, this, OnCommandDataChanged);
        }

        private void OnCommandDataChanged(object sender, EventArgs e)
        {
            var command = sender as RichDelegateCommand;
            if (command == null)
                return;

            if (command != Command)
            {
#if DEBUG
                Debugger.Break();
#endif
                return;
            }

            RefreshMenuItemFromItsRichCommand(this);
        }

        public static void ConfigureMenuItemFromCommand(MenuItem menuItem, RichDelegateCommand command)
        {
            if (menuItem is MenuItemRichCommand)
            {
                ((MenuItemRichCommand)menuItem).SetRichCommand(command);
            }
            else if (menuItem is TwoStateMenuItemRichCommand)
            {
                ((TwoStateMenuItemRichCommand)menuItem).SetRichCommand(command);
            }
            else
            {
                menuItem.Command = command;
                RefreshMenuItemFromItsRichCommand(menuItem);

#if DEBUG
                Debugger.Break();
#endif
            }
        }

        public static void RefreshMenuItemFromItsRichCommand(MenuItem menuItem)
        {
            var command = menuItem.Command as RichDelegateCommand;
            if (command == null)
            {
#if DEBUG
                Debugger.Break();
#endif
                return;
            }


            menuItem.Header = command.ShortDescription;
            menuItem.ToolTip = command.LongDescription + (command.KeyGesture != null ? " " + command.KeyGestureText + " " : "");

            menuItem.SetValue(AutomationProperties.NameProperty, UserInterfaceStrings.EscapeMnemonic(command.ShortDescription) + " / " + menuItem.ToolTip);
            //button.SetValue(AutomationProperties.HelpTextProperty, command.ShortDescription);

            menuItem.InputGestureText = command.KeyGestureText;

            //Image image = command.IconProvider.IconSmall;
            //image.Margin = new Thickness(0, 2, 0, 2);
            //image.VerticalAlignment = VerticalAlignment.Center;


            if (command.HasIcon)
            {
                var iconProvider = command.IconProviderNotShared;

                iconProvider.IconMargin_Small = new Thickness(0, 2, 0, 2);

                //menuItem.Icon = image;

                var binding = new Binding
                                  {
                                      Mode = BindingMode.OneWay,
                                      Source = iconProvider,
                                      Path =
                                          new PropertyPath(
                                          PropertyChangedNotifyBase.GetMemberName(() => iconProvider.IconSmall))
                                  };

                var expr = menuItem.SetBinding(MenuItem.IconProperty, binding);
            }
        }
    }

    public class TwoStateMenuItemRichCommand : MenuItem
    {

        public static readonly DependencyProperty InputBindingManagerProperty =
            DependencyProperty.Register("InputBindingManager",
                                        typeof(IInputBindingManager),
                                        typeof(TwoStateMenuItemRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnInputBindingManagerChanged)));

        private static void OnInputBindingManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ;
        }

        public IInputBindingManager InputBindingManager
        {
            get
            {
                return (IInputBindingManager)GetValue(InputBindingManagerProperty);
            }
            set
            {
                SetValue(InputBindingManagerProperty, value);
            }
        }


        public static readonly DependencyProperty RichCommandOneProperty =
            DependencyProperty.Register("RichCommandOne",
                                        typeof(RichDelegateCommand),
                                        typeof(TwoStateMenuItemRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandOneChanged)));

        private static void OnRichCommandOneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //ButtonRichCommand.OnRichCommandChanged(d, e);
        }

        public RichDelegateCommand RichCommandOne
        {
            get
            {
                return (RichDelegateCommand)GetValue(RichCommandOneProperty);
            }
            set
            {
                SetValue(RichCommandOneProperty, value);
            }
        }

        public static readonly DependencyProperty RichCommandTwoProperty =
            DependencyProperty.Register("RichCommandTwo",
                                        typeof(RichDelegateCommand),
                                        typeof(TwoStateMenuItemRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandTwoChanged)));

        private static void OnRichCommandTwoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //ButtonRichCommand.OnRichCommandChanged(d, e);
        }

        public RichDelegateCommand RichCommandTwo
        {
            get
            {
                return (RichDelegateCommand)GetValue(RichCommandTwoProperty);
            }
            set
            {
                SetValue(RichCommandTwoProperty, value);
            }
        }
        public static readonly DependencyProperty RichCommandActiveProperty =
                    DependencyProperty.Register("RichCommandActive",
                                                typeof(Boolean),
                                                typeof(TwoStateMenuItemRichCommand),
                                                new PropertyMetadata(true, OnRichCommandActiveChanged));

        public static void OnRichCommandActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var menuItem = d as TwoStateMenuItemRichCommand;
            if (menuItem == null)
            {
                return;
            }
            var choice = (Boolean)e.NewValue;

            ConfigureTwoStateMenuItemRichCommand(menuItem, choice);
        }

        public static void ConfigureTwoStateMenuItemRichCommand(TwoStateMenuItemRichCommand menuItem, bool choice)
        {
            //RichDelegateCommand command = menuItem.RichCommandOne;

            //if (command.KeyGesture == null && menuItem.RichCommandTwo.KeyGesture != null)
            //{
            //    command.KeyGestureText = menuItem.RichCommandTwo.KeyGestureText;
            //}

            if (menuItem.InputBindingManager != null)
            {
                if (choice)
                {
                    if (true
                        //KeyGestureString.AreEqual(command.KeyGesture, menuItem.RichCommandTwo.KeyGesture)
                        //&& command.KeyGesture.Equals(menuItem.RichCommandTwo.KeyGesture)
                        )
                    {
                        if (menuItem.RichCommandTwo.KeyGesture != null)
                            menuItem.InputBindingManager.RemoveInputBinding(menuItem.RichCommandTwo.KeyBinding);

                        if (menuItem.RichCommandOne.KeyGesture != null)
                            menuItem.InputBindingManager.AddInputBinding(menuItem.RichCommandOne.KeyBinding);
                    }
                }
                else
                {
                    //command = menuItem.RichCommandTwo;

                    //if (command.KeyGesture == null && menuItem.RichCommandOne.KeyGesture != null)
                    //{
                    //    command.KeyGestureText = menuItem.RichCommandOne.KeyGestureText;
                    //}

                    if (true
                        //&& KeyGestureString.AreEqual(command.KeyGesture, menuItem.RichCommandOne.KeyGesture)
                        //&& command.KeyGesture.Equals(menuItem.RichCommandOne.KeyGesture)
                        )
                    {
                        if (menuItem.RichCommandOne.KeyGesture != null)
                            menuItem.InputBindingManager.RemoveInputBinding(menuItem.RichCommandOne.KeyBinding);

                        if (menuItem.RichCommandTwo.KeyGesture != null)
                            menuItem.InputBindingManager.AddInputBinding(menuItem.RichCommandTwo.KeyBinding);
                    }
                }
            }

            MenuItemRichCommand.ConfigureMenuItemFromCommand(menuItem,
                            choice ? menuItem.RichCommandOne : menuItem.RichCommandTwo);
        }

        /// <summary>
        /// True => RichCommandOne (default one)
        /// False => RichCommandTwo (alternative one)
        /// </summary>
        public Boolean RichCommandActive
        {
            get
            {
                return (Boolean)GetValue(RichCommandActiveProperty);
            }
            set
            {
                SetValue(RichCommandActiveProperty, value);
            }
        }


        public static void SetRichCommand(RichDelegateCommand command, TwoStateMenuItemRichCommand menuitem, EventHandler dataChangedEventCallback)
        {
            if (menuitem.Command == command)
                return;

            if (menuitem.Command != null
                && menuitem.Command is RichDelegateCommand
                && ((RichDelegateCommand)menuitem.Command).DataChangedHasHandlers)
            {
                ((RichDelegateCommand)menuitem.Command).DataChanged -= dataChangedEventCallback;
            }

            menuitem.Command = command;

            MenuItemRichCommand.RefreshMenuItemFromItsRichCommand(menuitem);

            command.DataChanged += dataChangedEventCallback;
        }

        public void SetRichCommand(RichDelegateCommand command)
        {
            SetRichCommand(command, this, OnCommandDataChanged);
        }

        private void OnCommandDataChanged(object sender, EventArgs e)
        {
            var command = sender as RichDelegateCommand;
            if (command == null)
                return;

            if (command != Command)
            {
#if DEBUG
                Debugger.Break();
#endif
                return;
            }

            MenuItemRichCommand.RefreshMenuItemFromItsRichCommand(this);
        }
    }

    public class TwoStateMenuItemRichCommand_DataContextWrapper // : PropertyChangedNotifyBase
    {
        public object RichCommandActive_BindingSource
        {
            get;
            set;
        }

        public IInputBindingManager InputBindingManager
        {
            get;
            set;
        }

        public RichDelegateCommand RichCommandOne
        {
            get;
            set;
        }

        public RichDelegateCommand RichCommandTwo
        {
            get;
            set;
        }

        public Boolean RichCommandActive
        {
            get;
            set;
        }

        public Expression<Func<object>> RichCommandActive_BindingPropertyPathLambdaExpr
        {
            get;
            set;
        }

        //private IInputBindingManager m_InputBindingManager;
        //public IInputBindingManager InputBindingManager
        //{
        //    get { return m_InputBindingManager; }
        //    set
        //    {
        //        if (value == m_InputBindingManager) { return; }
        //        m_InputBindingManager = value;
        //        RaisePropertyChanged(() => InputBindingManager);
        //    }
        //}

        //private RichDelegateCommand m_RichCommandOne;
        //public RichDelegateCommand RichCommandOne
        //{
        //    get { return m_RichCommandOne; }
        //    set
        //    {
        //        if (value == m_RichCommandOne) { return; }
        //        m_RichCommandOne = value;
        //        RaisePropertyChanged(() => RichCommandOne);
        //    }
        //}

        //private RichDelegateCommand m_RichCommandTwo;
        //public RichDelegateCommand RichCommandTwo
        //{
        //    get { return m_RichCommandTwo; }
        //    set
        //    {
        //        if (value == m_RichCommandTwo) { return; }
        //        m_RichCommandTwo = value;
        //        RaisePropertyChanged(() => RichCommandTwo);
        //    }
        //}

        //private Boolean m_RichCommandActive;
        //public Boolean RichCommandActive
        //{
        //    get { return m_RichCommandActive; }
        //    set
        //    {
        //        if (value == m_RichCommandActive) { return; }
        //        m_RichCommandActive = value;
        //        RaisePropertyChanged(() => RichCommandActive);
        //    }
        //}
    }
}