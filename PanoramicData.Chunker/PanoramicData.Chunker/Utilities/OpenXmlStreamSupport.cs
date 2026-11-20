namespace PanoramicData.Chunker.Utilities;

/// <summary>
/// Shared stream handling for the Open XML chunkers (DOCX, XLSX, PPTX).
/// </summary>
internal static class OpenXmlStreamSupport
{
	public static async Task<bool> IsZipThatOpensAsync(Stream documentStream, Func<Stream, bool> opens, CancellationToken cancellationToken)
	{
		try
		{
			var originalPosition = documentStream.Position;

			var buffer = new byte[4];
			var bytesRead = await documentStream.ReadAsync(buffer, cancellationToken);

			documentStream.Position = originalPosition;

			return bytesRead == 4 && HasZipSignature(buffer) && opens(documentStream);
		}
		catch
		{
			return false;
		}
	}

	public static bool Opens<T>(Stream documentStream, Func<Stream, T> open, Func<T, bool> hasMainPart)
		where T : IDisposable
	{
		try
		{
			using var doc = open(documentStream);
			return hasMainPart(doc);
		}
		catch
		{
			return false;
		}
	}

	public static async Task<MemoryStream> CopyToSeekableAsync(Stream documentStream, CancellationToken cancellationToken)
	{
		var memoryStream = new MemoryStream();
		await documentStream.CopyToAsync(memoryStream, cancellationToken);
		memoryStream.Position = 0;
		return memoryStream;
	}

	private static bool HasZipSignature(byte[] buffer)
		=> buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04;
}