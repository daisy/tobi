using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Tobi.Common.UI
{
    public class SortableGridViewColumn : GridViewColumn
    {



        public string SortPropertyName
        {

            get { return (string)GetValue(SortPropertyNameProperty); }

            set { SetValue(SortPropertyNameProperty, value); }

        }


        public static readonly DependencyProperty SortPropertyNameProperty =

            DependencyProperty.Register("SortPropertyName", typeof(string), typeof(SortableGridViewColumn), new UIPropertyMetadata(""));





        public bool IsDefaultSortColumn
        {

            get { return (bool)GetValue(IsDefaultSortColumnProperty); }

            set { SetValue(IsDefaultSortColumnProperty, value); }

        }



        public static readonly DependencyProperty IsDefaultSortColumnProperty =

            DependencyProperty.Register("IsDefaultSortColumn", typeof(bool), typeof(SortableGridViewColumn), new UIPropertyMetadata(false));



    }
    public class SortableListView2 : ListView
    {

        SortableGridViewColumn lastSortedOnColumn = null;

        ListSortDirection lastDirection = ListSortDirection.Ascending;





        #region New Dependency Properties



        public string ColumnHeaderSortedAscendingTemplate
        {

            get { return (string)GetValue(ColumnHeaderSortedAscendingTemplateProperty); }

            set { SetValue(ColumnHeaderSortedAscendingTemplateProperty, value); }

        }


        public static readonly DependencyProperty ColumnHeaderSortedAscendingTemplateProperty =

            DependencyProperty.Register("ColumnHeaderSortedAscendingTemplate", typeof(string), typeof(SortableListView2), new UIPropertyMetadata(""));





        public string ColumnHeaderSortedDescendingTemplate
        {

            get { return (string)GetValue(ColumnHeaderSortedDescendingTemplateProperty); }

            set { SetValue(ColumnHeaderSortedDescendingTemplateProperty, value); }

        }


        public static readonly DependencyProperty ColumnHeaderSortedDescendingTemplateProperty =

            DependencyProperty.Register("ColumnHeaderSortedDescendingTemplate", typeof(string), typeof(SortableListView2), new UIPropertyMetadata(""));





        public string ColumnHeaderNotSortedTemplate
        {

            get { return (string)GetValue(ColumnHeaderNotSortedTemplateProperty); }

            set { SetValue(ColumnHeaderNotSortedTemplateProperty, value); }

        }



        public static readonly DependencyProperty ColumnHeaderNotSortedTemplateProperty =

            DependencyProperty.Register("ColumnHeaderNotSortedTemplate", typeof(string), typeof(SortableListView2), new UIPropertyMetadata(""));





        #endregion


        protected void defaultSort()
        {

            if (ItemsSource == null) return;

            this.SelectedIndex = 0;

            GridView gridView = this.View as GridView;

            if (gridView != null)
            {
                SortableGridViewColumn sortableGridViewColumn = null;

                foreach (GridViewColumn gridViewColumn in gridView.Columns)
                {

                    sortableGridViewColumn = gridViewColumn as SortableGridViewColumn;

                    if (sortableGridViewColumn != null)
                    {

                        if (sortableGridViewColumn.IsDefaultSortColumn)
                        {

                            break;

                        }

                        sortableGridViewColumn = null;

                    }

                }

                if (sortableGridViewColumn != null)
                {

                    lastSortedOnColumn = sortableGridViewColumn;

                    Sort(sortableGridViewColumn.SortPropertyName, ListSortDirection.Ascending);



                    if (!String.IsNullOrEmpty(this.ColumnHeaderSortedAscendingTemplate))
                    {

                        sortableGridViewColumn.HeaderTemplate = this.TryFindResource(ColumnHeaderSortedAscendingTemplate) as DataTemplate;

                    }



                    this.SelectedIndex = 0;

                }

            }
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            defaultSort();
        }


        protected override void OnInitialized(EventArgs e)
        {
            this.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(GridViewColumnHeaderClickedHandler));

            defaultSort();

            base.OnInitialized(e);

        }


        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {

            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;


            if (headerClicked != null && headerClicked.Role != GridViewColumnHeaderRole.Padding)
            {
                SortableGridViewColumn sortableGridViewColumn = (headerClicked.Column) as SortableGridViewColumn;

                if (sortableGridViewColumn != null && !String.IsNullOrEmpty(sortableGridViewColumn.SortPropertyName))
                {



                    ListSortDirection direction;

                    bool newSortColumn = false;

                    if (lastSortedOnColumn == null

                        || String.IsNullOrEmpty(lastSortedOnColumn.SortPropertyName)

                        || !String.Equals(sortableGridViewColumn.SortPropertyName, lastSortedOnColumn.SortPropertyName, StringComparison.InvariantCultureIgnoreCase))
                    {

                        newSortColumn = true;

                        direction = ListSortDirection.Ascending;

                    }

                    else
                    {

                        if (lastDirection == ListSortDirection.Ascending)
                        {

                            direction = ListSortDirection.Descending;

                        }

                        else
                        {

                            direction = ListSortDirection.Ascending;

                        }

                    }

                    string sortPropertyName = sortableGridViewColumn.SortPropertyName;

                    Sort(sortPropertyName, direction);



                    if (direction == ListSortDirection.Ascending)
                    {

                        if (!String.IsNullOrEmpty(this.ColumnHeaderSortedAscendingTemplate))
                        {

                            sortableGridViewColumn.HeaderTemplate = this.TryFindResource(ColumnHeaderSortedAscendingTemplate) as DataTemplate;

                        }

                        else
                        {

                            sortableGridViewColumn.HeaderTemplate = null;

                        }

                    }

                    else
                    {

                        if (!String.IsNullOrEmpty(this.ColumnHeaderSortedDescendingTemplate))
                        {

                            sortableGridViewColumn.HeaderTemplate = this.TryFindResource(ColumnHeaderSortedDescendingTemplate) as DataTemplate;

                        }

                        else
                        {

                            sortableGridViewColumn.HeaderTemplate = null;

                        }

                    }

                    if (newSortColumn && lastSortedOnColumn != null)
                    {

                        if (!String.IsNullOrEmpty(this.ColumnHeaderNotSortedTemplate))
                        {

                            lastSortedOnColumn.HeaderTemplate = this.TryFindResource(ColumnHeaderNotSortedTemplate) as DataTemplate;

                        }

                        else
                        {

                            lastSortedOnColumn.HeaderTemplate = null;

                        }

                    }

                    lastSortedOnColumn = sortableGridViewColumn;

                }

            }

        }


        private void Sort(string sortBy, ListSortDirection direction)
        {
            if (ItemsSource == null) return;


            ICollectionView dataView = CollectionViewSource.GetDefaultView(this.ItemsSource);

            if (dataView == null) return;


            lastDirection = direction;


            dataView.SortDescriptions.Clear();

            SortDescription sd = new SortDescription(sortBy, direction);

            dataView.SortDescriptions.Add(sd);

            dataView.Refresh();

        }

    }






    //public class SortableListView : ListView
    //{
    //    private GridViewColumnHeader _lastHeaderClicked = null;
    //    private ListSortDirection _lastDirection = ListSortDirection.Ascending;

    //    //public SortableListView()
    //    //{
    //    //    // See OnInitialized()

    //    //    //this.AddHandler(
    //    //    //    ButtonBase.ClickEvent,
    //    //    //    new RoutedEventHandler(GridViewColumnHeaderClickedHandler));
    //    //}

    //    private void Sort(string sortBy, ListSortDirection direction)
    //    {
    //        ICollectionView dataView =
    //          CollectionViewSource.GetDefaultView(this.ItemsSource);

    //        if (dataView != null)
    //        {
    //            dataView.SortDescriptions.Clear();
    //            SortDescription sd = new SortDescription(sortBy, direction);

    //            try
    //            {
    //                dataView.SortDescriptions.Add(sd);
    //            }
    //            catch (InvalidOperationException)
    //            {
    //                return;
    //            }
    //            dataView.Refresh();
    //        }
    //    }

    //    private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
    //    {
    //        GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
    //        ListSortDirection direction;

    //        if (headerClicked != null &&
    //            headerClicked.Role != GridViewColumnHeaderRole.Padding)
    //        {
    //            if (_lastHeaderClicked != null)
    //            {
    //                _lastHeaderClicked.Column.HeaderTemplate = null;
    //            }

    //            if (headerClicked != _lastHeaderClicked)
    //            {
    //                direction = ListSortDirection.Ascending;
    //            }
    //            else
    //            {
    //                if (_lastDirection == ListSortDirection.Ascending)
    //                {
    //                    direction = ListSortDirection.Descending;
    //                }
    //                else
    //                {
    //                    direction = ListSortDirection.Ascending;
    //                }
    //            }

    //            // see if we have an attached SortPropertyName value
    //            string sortBy = GetSortPropertyName(headerClicked.Column);
    //            if (string.IsNullOrEmpty(sortBy))
    //            {
    //                return;

    //                // otherwise use the column header name
    //                //sortBy = headerClicked.Column.Header as string;
    //            }


    //            if (direction == ListSortDirection.Ascending)
    //            {
    //                headerClicked.Column.HeaderTemplate =
    //                  Application.Current.Resources["ColumnHeaderTemplateArrowUp"] as DataTemplate;
    //            }
    //            else
    //            {
    //                headerClicked.Column.HeaderTemplate =
    //                  Application.Current.Resources["ColumnHeaderTemplateArrowDown"] as DataTemplate;
    //            }

    //            Sort(sortBy, direction);

    //            _lastHeaderClicked = headerClicked;
    //            _lastDirection = direction;
    //        }
    //    }

    //    protected override void OnInitialized(EventArgs e)
    //    {
    //        this.AddHandler(GridViewColumnHeader.ClickEvent,
    //            new RoutedEventHandler(GridViewColumnHeaderClickedHandler));

    //        if (ItemsSource == null) return;

    //        this.SelectedIndex = 0;

    //        var gridView = this.View as GridView;

    //        if (gridView != null)
    //        {
    //            GridViewColumn found = null;
    //            foreach (GridViewColumn gridViewColumn in gridView.Columns)
    //            {
    //                if (gridViewColumn != null)
    //                {
    //                    if (GetIsDefaultSortColumn(gridViewColumn))
    //                    {
    //                        found = gridViewColumn;
    //                        break;
    //                    }
    //                }
    //            }

    //            if (found != null)
    //            {
    //                Sort(GetSortPropertyName(found), ListSortDirection.Ascending);

    //                this.SelectedIndex = 0;
    //            }
    //        }

    //        base.OnInitialized(e);
    //    }

    //    //public bool IsDefaultSortColumn
    //    //{

    //    //    get { return (bool)GetValue(IsDefaultSortColumnProperty); }

    //    //    set { SetValue(IsDefaultSortColumnProperty, value); }

    //    //}

    //    public static readonly DependencyProperty IsDefaultSortColumnProperty =
    //        DependencyProperty.RegisterAttached("IsDefaultSortColumn", typeof(bool), typeof(SortableListView)); //, new UIPropertyMetadata(false));

    //    public static bool GetIsDefaultSortColumn(GridViewColumn obj)
    //    {
    //        return (bool)obj.GetValue(IsDefaultSortColumnProperty);
    //    }

    //    public static void SetIsDefaultSortColumn(GridViewColumn obj, bool value)
    //    {
    //        obj.SetValue(IsDefaultSortColumnProperty, value);
    //    }


    //    public static readonly DependencyProperty SortPropertyNameProperty =
    //        DependencyProperty.RegisterAttached("SortPropertyName", typeof(string), typeof(SortableListView));

    //    public static string GetSortPropertyName(GridViewColumn obj)
    //    {
    //        return (string)obj.GetValue(SortPropertyNameProperty);
    //    }

    //    public static void SetSortPropertyName(GridViewColumn obj, string value)
    //    {
    //        obj.SetValue(SortPropertyNameProperty, value);
    //    }
    //}



}
