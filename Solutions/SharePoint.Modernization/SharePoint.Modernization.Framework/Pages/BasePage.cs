﻿using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SharePoint.Modernization.Framework.Pages
{

    /// <summary>
    /// Base class for the page analyzers
    /// </summary>
    public abstract class BasePage
    {
        public ListItem page;
        public ClientContext cc;
        public PageTransformation pageTransformation;

        #region construction
        /// <summary>
        /// Constructs the base page class instance
        /// </summary>
        /// <param name="page">page ListItem</param>
        /// <param name="pageTransformation">page transformation model to use for extraction or transformation</param>
        public BasePage(ListItem page, PageTransformation pageTransformation)
        {
            this.page = page;
            this.cc = (page.Context as ClientContext);
            this.cc.RequestTimeout = Timeout.Infinite;

            this.pageTransformation = pageTransformation;
        }
        #endregion

        /// <summary>
        /// Get's the type of the web part
        /// </summary>
        /// <param name="webPartXml">Web part xml to analyze</param>
        /// <returns>Type of the web part as fully qualified name</returns>
        public string GetType(string webPartXml)
        {
            string type = "Unknown";

            if (!string.IsNullOrEmpty(webPartXml))
            {
                var xml = XElement.Parse(webPartXml);
                var xmlns = xml.XPathSelectElement("*").GetDefaultNamespace();
                if (xmlns.NamespaceName.Equals("http://schemas.microsoft.com/WebPart/v3", StringComparison.InvariantCultureIgnoreCase))
                {
                    type = xml.Descendants(xmlns + "type").FirstOrDefault().Attribute("name").Value;
                }
                else if (xmlns.NamespaceName.Equals("http://schemas.microsoft.com/WebPart/v2", StringComparison.InvariantCultureIgnoreCase))
                {
                    type = $"{xml.Descendants(xmlns + "TypeName").FirstOrDefault().Value}, {xml.Descendants(xmlns + "Assembly").FirstOrDefault().Value}";
                }
            }

            return type;
        }

        /// <summary>
        /// Get's the type of the web part by detecting if from the available properties
        /// </summary>
        /// <param name="properties">Web part properties to analyze</param>
        /// <returns>Type of the web part as fully qualified name</returns>
        public string GetTypeFromProperties(PropertyValues properties)
        {
            // Check for XSLTListView web part
            string[] xsltWebPart = new string[] { "ListUrl", "ListId", "Xsl", "JSLink", "ShowTimelineIfAvailable" };                        
            if (CheckWebPartProperties(xsltWebPart, properties))
            {
                return "Microsoft.SharePoint.WebPartPages.XsltListViewWebPart, Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            }

            // Check for ListView web part
            string[] listWebPart = new string[] { "ListViewXml", "ListName", "ListId", "ViewContentTypeId", "PageType" };
            if (CheckWebPartProperties(listWebPart, properties))
            {
                return "Microsoft.SharePoint.WebPartPages.ListViewWebPart, Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            }

            // check for Media web part
            string[] mediaWebPart = new string[] { "AutoPlay", "MediaSource", "Loop", "IsPreviewImageSourceOverridenForVideoSet", "PreviewImageSource" };
            if (CheckWebPartProperties(mediaWebPart, properties))
            {
                return "Microsoft.SharePoint.Publishing.WebControls.MediaWebPart, Microsoft.SharePoint.Publishing, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            }

            // check for SlideShow web part
            string[] slideShowWebPart = new string[] { "LibraryGuid", "Layout", "Speed", "ShowToolbar", "ViewGuid" };
            if (CheckWebPartProperties(slideShowWebPart, properties))
            {
                return "Microsoft.SharePoint.WebPartPages.PictureLibrarySlideshowWebPart, Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            }

            // check for Chart web part
            string[] chartWebPart = new string[] { "ConnectionPointEnabled", "ChartXml", "DataBindingsString", "DesignerChartTheme" };
            if (CheckWebPartProperties(chartWebPart, properties))
            {
                return "Microsoft.Office.Server.WebControls.ChartWebPart, Microsoft.Office.Server.Chart, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            }

            // check for Site Members web part
            string[] membersWebPart = new string[] { "NumberLimit", "DisplayType", "MembershipGroupId", "Toolbar" };
            if (CheckWebPartProperties(membersWebPart, properties))
            {
                return "Microsoft.SharePoint.WebPartPages.MembersWebPart, Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            }

            // check for Silverlight web part
            string[] silverlightWebPart = new string[] { "MinRuntimeVersion", "WindowlessMode", "CustomInitParameters", "Url", "ApplicationXml" };
            if (CheckWebPartProperties(silverlightWebPart, properties))
            {
                return "Microsoft.SharePoint.WebPartPages.SilverlightWebPart, Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            }

            // check for Add-in Part web part
            string[] addinPartWebPart = new string[] { "FeatureId", "ProductWebId", "ProductId" };
            if (CheckWebPartProperties(addinPartWebPart, properties))
            {
                return "Microsoft.SharePoint.WebPartPages.ClientWebPart, Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            }

            // check for Script Editor web part
            string[] scriptEditorWebPart = new string[] { "Content"};
            if (CheckWebPartProperties(scriptEditorWebPart, properties))
            {
                return "Microsoft.SharePoint.WebPartPages.ScriptEditorWebPart, Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            }

            // This needs to be last, but we still pages with sandbox user code web parts on them
            string[] sandboxWebPart = new string[] { "CatalogIconImageUrl", "AllowEdit", "TitleIconImageUrl", "ExportMode" };
            if (CheckWebPartProperties(sandboxWebPart, properties))
            {
                return "Microsoft.SharePoint.WebPartPages.SPUserCodeWebPart, Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            }

            return "NonExportable_Unidentified";
        }

        private bool CheckWebPartProperties(string[] propertiesToCheck, PropertyValues properties)
        {
            bool isWebPart = true;
            foreach (var wpProp in propertiesToCheck)
            {
                if (!properties.FieldValues.ContainsKey(wpProp))
                {
                    isWebPart = false;
                    break;
                }
            }

            return isWebPart;
        }

        /// <summary>
        /// Checks the PageTransformation XML data to know which properties need to be kept for the given web part and collects their values
        /// </summary>
        /// <param name="properties">Properties collection retrieved when we loaded the web part</param>
        /// <param name="webPartType">Type of the web part</param>
        /// <param name="webPartXml">Web part XML</param>
        /// <returns>Collection of the requested property/value pairs</returns>
        public Dictionary<string, string> Properties(PropertyValues properties, string webPartType, string webPartXml)
        {
            Dictionary<string, string> propertiesToKeep = new Dictionary<string, string>();

            List<Property> propertiesToRetrieve = this.pageTransformation.BaseWebPart.Properties.ToList<Property>();
            var webPartProperties = this.pageTransformation.WebParts.Where(p => p.Type.Equals(webPartType, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (webPartProperties != null && webPartProperties.Properties != null)
            {
                propertiesToRetrieve.AddRange(webPartProperties.Properties.ToList<Property>());
            }

            if (string.IsNullOrEmpty(webPartXml))
            {
                // Special case where we did not have export rights for the web part XML, assume this is a V3 web part
                foreach (var property in propertiesToRetrieve)
                {
                    if (!string.IsNullOrEmpty(property.Name) && properties.FieldValues.ContainsKey(property.Name))
                    {
                        propertiesToKeep.Add(property.Name, properties[property.Name] != null ? properties[property.Name].ToString() : "");
                    }
                }
            }
            else
            {
                var xml = XElement.Parse(webPartXml);
                var xmlns = xml.XPathSelectElement("*").GetDefaultNamespace();
                if (xmlns.NamespaceName.Equals("http://schemas.microsoft.com/WebPart/v3", StringComparison.InvariantCultureIgnoreCase))
                {
                    // the retrieved properties are sufficient
                    foreach (var property in propertiesToRetrieve)
                    {
                        if (!string.IsNullOrEmpty(property.Name) && properties.FieldValues.ContainsKey(property.Name))
                        {
                            propertiesToKeep.Add(property.Name, properties[property.Name] != null ? properties[property.Name].ToString() : "");
                        }
                    }
                }
                else if (xmlns.NamespaceName.Equals("http://schemas.microsoft.com/WebPart/v2", StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (var property in propertiesToRetrieve)
                    {
                        if (!string.IsNullOrEmpty(property.Name))
                        {
                            if (properties.FieldValues.ContainsKey(property.Name))
                            {
                                propertiesToKeep.Add(property.Name, properties[property.Name] != null ? properties[property.Name].ToString() : "");
                            }
                            else
                            {
                                // check XMl for property
                                var v2Element = xml.Descendants(xmlns + property.Name).FirstOrDefault();
                                if (v2Element != null)
                                {
                                    propertiesToKeep.Add(property.Name, v2Element.Value);
                                }
                            }
                        }
                    }
                }
            }

            return propertiesToKeep;
        }


    }
}
