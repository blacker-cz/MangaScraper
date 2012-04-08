using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections;
using System.Windows.Controls;

namespace Blacker.MangaScraper.Helpers
{
    public class ListBoxExtension
    {
        #region Attached Property Declaration

        public static readonly DependencyProperty SelectedItemsSourceProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItemsSource",
                typeof(IList),
                typeof(ListBoxExtension),
                new PropertyMetadata(
                    null,
                    new PropertyChangedCallback(OnSelectedItemsSourceChanged)));

        #endregion
        #region Attached Property Accessors
        public static IList GetSelectedItemsSource(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException("element"); return (IList)element.GetValue(SelectedItemsSourceProperty);

        }
        public static void SetSelectedItemsSource(DependencyObject element, IList value)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            element.SetValue(SelectedItemsSourceProperty, value);
        }

        #endregion

        #region IsResynchingProperty

        private static readonly DependencyPropertyKey IsResynchingPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "IsResynching",
                typeof(bool),
                typeof(ListBoxExtension),
                new PropertyMetadata(false));

        #endregion
        #region Private Static Methods
        private static void OnSelectedItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ListBox listBox = d as ListBox;
            if (listBox == null)

                throw new InvalidOperationException("The ListBoxExtension.SelectedItemsSource attached property can only be applied to ListBox controls.");

            listBox.SelectionChanged -= new SelectionChangedEventHandler(OnListBoxSelectionChanged);
            if (e.NewValue != null)
            {
                ListenForChanges(listBox);
            }
        }

        private static void ListenForChanges(ListBox listBox)
        {
            if (!listBox.IsInitialized)
            {
                EventHandler callback = null;

                callback = delegate
                {
                    listBox.Initialized -= callback;
                    ListenForChanges(listBox);
                };

                listBox.Initialized += callback;
                return;

            }
            listBox.SelectionChanged += new SelectionChangedEventHandler(OnListBoxSelectionChanged);

            ResynchList(listBox);
        }

        private static void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = sender as ListBox; if (listBox != null)
            {

                //BindingExpression bexp = listBox.GetBindingExpression(SelectedItemsSourceProperty);

                //bexp.UpdateSource();
                bool isResynching = (bool)listBox.GetValue(IsResynchingPropertyKey.DependencyProperty);

                if (isResynching)
                    return;

                IList list = GetSelectedItemsSource(listBox);
                if (list != null)
                {
                    foreach (object obj in e.RemovedItems)
                    {
                        if (list.Contains(obj))
                            list.Remove(obj);
                    }
                    foreach (object obj in e.AddedItems)
                    {
                        if (!list.Contains(obj))
                            list.Add(obj);
                    }
                }
            }
        }

        private static void ResynchList(ListBox listBox)
        {
            if (listBox != null)
            {
                listBox.SetValue(IsResynchingPropertyKey, true);

                IList list = GetSelectedItemsSource(listBox);
                if (listBox.SelectionMode == SelectionMode.Single)
                {
                    listBox.SelectedItem = null; if (list != null)
                    {
                        if (list.Count > 1)
                        {
                            // There is more than one item selected, but the listbox is in Single selection mode
                            throw new InvalidOperationException("ListBox is in Single selection mode, but was given more than one selected value.");
                        }
                        if (list.Count == 1)
                            listBox.SelectedItem = list[0];
                    }
                }
                else
                {
                    listBox.SelectedItems.Clear();
                    if (list != null)
                    {
                        foreach (object obj in listBox.Items)
                        {
                            if (list.Contains(obj))
                                listBox.SelectedItems.Add(obj);
                        }
                    }
                }
                listBox.SetValue(IsResynchingPropertyKey, false);
            }
        }
        #endregion
    }
}
