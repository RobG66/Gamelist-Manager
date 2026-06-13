# Changelog

## [Unreleased]
### Fixed
- **API Clients:** Fixed an issue where EmuMovies, ScreenScraper, and ArcadeDB APIs were not properly URL-encoding user credentials (passwords, User IDs) and ROM names. This ensures scraping and authentication no longer fail when inputs contain special characters (e.g., `+`, `&`, `%`, spaces).
- **EmuMovies:** Removed unnecessary `async`/`await` overhead from URL generation methods that were running synchronously.
