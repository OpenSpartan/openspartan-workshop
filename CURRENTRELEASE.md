# OpenSpartan Workshop 1.0.7 (`CYLIX-06082024`)

- [#30] Prevent auth loops if incorrect release number is used.
- [#38] Battlepass data now correctly renders on smaller screens, allowing scrolling.
- [#39] Removes the odd cross-out line in the calendar view.
- [#41] Fixes average life positioning, ensuring that it can't cause overflow.
- Improved image fallback for The Exchange, so that missing items now render properly.
- Season calendar now includes background images for each event, operation, and battle pass season.
- Calendar colors are now easier to read.
- Fixes how ranked match percentage is calculated, now showing proper values for next level.
- Home page now can be scrolled on smaller screens.
- Inside match metadata, medals and ranked counterfactuals correctly flow when screen is resized.
- The app now correctly reacts at startup to an error with authentication token acquisition. A message is shown if that is not possible.
- General performance optimizations and maintainability cleanup.
- Fixed an issue where duplicate ranked playlist may render in the **Ranked Progression** view.
- Fixed an issue where duplicate items may render in the **Exchange** view.
- Massive speed up to the **Operations** view load times - the app no longer issues unnecessary REST API calls to get currency data.

Refer to [**getting started guide**](https://openspartan.com/docs/workshop/guides/get-started/) to start using OpenSpartan Workshop.
