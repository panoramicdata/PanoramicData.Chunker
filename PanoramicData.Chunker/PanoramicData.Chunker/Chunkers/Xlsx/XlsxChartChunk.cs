using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Xlsx;

/// <summary>
/// Represents a chart in an Excel spreadsheet.
/// </summary>
public class XlsxChartChunk : VisualChunk
{
	/// <summary>
	/// Gets or sets the chart title.
	/// </summary>
	public string? ChartTitle { get; set; }

	/// <summary>
	/// Gets or sets the worksheet name containing this chart.
	/// </summary>
	public string SheetName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the chart type (e.g., "Bar", "Line", "Pie").
	/// </summary>
	public string ChartType { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the data range for the chart (e.g., "A1:D10").
	/// </summary>
	public string? DataRange { get; set; }

	/// <summary>
	/// Gets or sets the series names in the chart.
	/// </summary>
	public List<string> SeriesNames { get; set; } = [];

	/// <summary>
	/// Gets or sets the X-axis title.
	/// </summary>
	public string? XAxisTitle { get; set; }

	/// <summary>
	/// Gets or sets the Y-axis title.
	/// </summary>
	public string? YAxisTitle { get; set; }

	/// <summary>
	/// Gets or sets whether the chart has a legend.
	/// </summary>
	public bool HasLegend { get; set; }
}
