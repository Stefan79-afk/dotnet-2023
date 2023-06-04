using Google.Protobuf.Collections;
using OSMPBF;

namespace OSMDataParser;

public class HeaderBlock
{
    public HeaderBlock(Blob blob)
    {
        var osmHeaderBlock = Detail.DeserializeContent<OSMPBF.HeaderBlock>(blob);

        OptionalFeatures = ExtractFeatures(osmHeaderBlock.OptionalFeatures);
        RequiredFeatures = ExtractFeatures(osmHeaderBlock.RequiredFeatures);
        BoundingBox = osmHeaderBlock.Bbox;
    }

    public Feature[] OptionalFeatures { get; }
    public Feature[] RequiredFeatures { get; }
    public HeaderBBox BoundingBox { get; }

    private Feature[] ExtractFeatures(RepeatedField<string> featureList)
    {
        var result = new Feature[featureList.Count];
        for (var i = 0; i < result.Length; ++i)
        {
            var feature = featureList[i];
            switch (feature)
            {
                default:
                {
                    result[i] = new Feature(Feature.Unknown, feature);
                    break;
                }
                case "OsmSchema-V0.6":
                {
                    result[i] = new Feature(Feature.OsmSchemaV06);
                    break;
                }
                case "DenseNodes":
                {
                    result[i] = new Feature(Feature.DenseNodes);
                    break;
                }
                case "HistoricalInformation":
                {
                    result[i] = new Feature(Feature.HistoricalInformation);
                    break;
                }
            }
        }

        return result;
    }
}