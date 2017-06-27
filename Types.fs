namespace EpisodeWebMvc

type DirectoryOptions() =
    member val KeyFileDirectory = ""
        with get, set
    member val DecodeTargetDirectory = ""
        with get, set
    member val ProcessedTargetDirectory = ""
        with get, set


type Episode = { airedEpisodeNumber: int; airedSeason: int; episodeName: string; firstAired: string }

type Show = { seriesName: string; id: int; }
type ProcessInfo = { episodename: string; episodenumber: int; show: string; season: int; file: string }
