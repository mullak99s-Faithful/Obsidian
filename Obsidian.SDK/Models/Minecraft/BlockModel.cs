using Newtonsoft.Json;

namespace Obsidian.SDK.Models.Minecraft
{
	public class BlockModel
	{
		[JsonProperty("credit")]
		public string Credit { get; set; }

		[JsonProperty("ambientocclusion")]
		public bool AmbientOcclusion { get; set; }

		[JsonProperty("parent")]
		public string Parent { get; set; }

		[JsonProperty("textures")]
		public Dictionary<string, string>? Textures { get; set; }

		[JsonProperty("elements")]
		public List<Element> Elements { get; set; }

		[JsonProperty("gui_light")]
		public string GuiLight { get; set; }

		[JsonProperty("display")]
		public Dictionary<string, GuiDisplay> Display { get; set; }

		public class Element
		{
			[JsonProperty("name")]
			public string Name { get; set; }

			[JsonProperty("from")]
			public List<float> From { get; set; }

			[JsonProperty("to")]
			public List<float> To { get; set; }

			[JsonProperty("rotation")]
			public Rotation Rotation { get; set; }

			[JsonProperty("shade")]
			public bool? Shade { get; set; }

			[JsonProperty("faces")]
			public Dictionary<string, Face> Faces { get; set; }
		}

		public class Rotation
		{
			[JsonProperty("origin")]
			public List<float> Origin { get; set; }

			[JsonProperty("axis")]
			public string Axis { get; set; }

			[JsonProperty("angle")]
			public float Angle { get; set; }

			[JsonProperty("rescale")]
			public bool? Rescale { get; set; }
		}

		public class Face
		{
			[JsonProperty("uv")]
			public List<float> UV { get; set; }

			[JsonProperty("texture")]
			public string Texture { get; set; }

			[JsonProperty("cullface")]
			public string Cullface { get; set; }

			[JsonProperty("rotation")]
			public int? Rotation { get; set; }

			[JsonProperty("tintindex")]
			public int? TintIndex { get; set; }
		}

		public class GuiDisplay
		{
			[JsonProperty("rotation")]
			public List<float> Rotation { get; set; }

			[JsonProperty("translation")]
			public List<float> Translation { get; set; }

			[JsonProperty("scale")]
			public List<float> Scale { get; set; }
		}

		public string Serialize()
		{
			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Ignore
			};
			return JsonConvert.SerializeObject(this, settings);
		}
	}
}
