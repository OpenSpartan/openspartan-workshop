# OpenSpartan Workshop 1.0.8 (`SLIPSPACE-06282024`)

This is a **hotfix release** meant to address minor stability issues caused by the operation changeover.

- Individual match stats acquisition now supports retries, in case the initial call fails and we still need to obtain proper match data. This is helpful for scenarios where we're using loose match searches and need to make sure that the proper data is captured when inserting in the DB.
- Update the logic for date parsing in operations to avoid a mislabeled operation timeframe.
- Ensure that percentages render correctly across cultures.

Refer to [**getting started guide**](https://openspartan.com/docs/workshop/guides/get-started/) to start using OpenSpartan Workshop.
