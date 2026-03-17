using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CustomTagHelpers.TagHelpers
{
    [HtmlTargetElement("cute")]
    public class CuteTagHelper : TagHelper
    {
        public string ImageLink { get; set; }
        public string AlternativeText { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "img";
            output.TagMode = TagMode.StartTagOnly;

            output.Attributes.SetAttribute("src", ImageLink);
            output.Attributes.SetAttribute("alt", AlternativeText);
        }
    }
}