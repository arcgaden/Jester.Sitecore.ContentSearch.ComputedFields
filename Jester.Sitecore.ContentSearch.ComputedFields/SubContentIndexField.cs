using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sitecore;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.ComputedFields;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;

namespace Jester.Sitecore.ContentSearch.ComputedFields
{
    public class SubContentIndexField : IComputedIndexField
    {
        public string FieldName { get; set; }

        public string ReturnType { get; set; }

        public object ComputeFieldValue(IIndexable indexable)
        {
            var sitecoreIndexable = indexable as SitecoreIndexableItem;

            if (sitecoreIndexable == null) { return null; }

            // find renderings with datasources set
            var item = sitecoreIndexable.Item;

            var itemLinks = Globals.LinkDatabase.GetReferences(item).Where(r => (r.SourceFieldID == FieldIDs.LayoutField
                             || r.SourceFieldID == FieldIDs.FinalLayoutField) && r.TargetDatabaseName == item.Database.Name);

            //Null check and filter any duplicate datasource items
            var customDataSources = itemLinks.Select(l => l.GetTargetItem()).Where(i => i != null).Distinct();

            // extract text from data sources
            var contentToAdd = customDataSources.SelectMany(GetItemContent).ToList();

            if (contentToAdd.Count == 0) { return null; }

            return string.Join(" ", contentToAdd);
        }

        /// <summary>
        /// Extracts textual content from an item's fields
        /// </summary>
        protected virtual IEnumerable<string> GetItemContent(Item dataSource)
        {
            foreach (Field field in dataSource.Fields)
            {
                if (!IndexOperationsHelper.IsTextField(new SitecoreItemDataField(field)))
                {
                    // Use this to handle Rich Text fields
                    string fieldValue = StripHtml(field.Value ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(fieldValue)) { yield return fieldValue; }
                }
            }
        }

        /// <summary>
        /// Regex taken from External Source via Stack Overflow
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private string StripHtml(string html)
        {
            string pattern = @"</?\w+((\s+\w+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)/?>";
            var expression = new Regex(pattern);
            return expression.Replace(html, String.Empty);
        }

    }

}