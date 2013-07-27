using System.Windows;
using System.Windows.Controls;

namespace Blacker.MangaScraper.Recent
{
    internal class RecentMangaDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate MangaRecordTemplate { get; set; }
        public DataTemplate RecentMangaTemplate { get; set; }


        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is RecentMangaRecord)
                return RecentMangaTemplate;

            return MangaRecordTemplate;
        }
    }
}
