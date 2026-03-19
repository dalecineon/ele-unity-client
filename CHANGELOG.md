## [1.0.0] - 23-07-2025
### First Release
- Added the CineonRestClient.cs script this allows the user to connect to the rest api.
- Included the functionality to check the ping to the server also.

## [1.0.1] - 19-08-2025
### Added Eye Data Storage Script
- I have added an Eye Data Storage script to supplement the CineonRestClient.cs
- This script shows how the data needs to be setup in order to send and receive data to and from the server.

## [1.0.2] - 21-08-2025
### Added Eye Data Prefab
- I have added an Eye Data Prefab which can be dragged into the scene and has the scripts setup.

## [1.0.3] - 21-08-2025
### Added Vive Prefab Setup as an example
- I have added a vive prefab setup for ease of use in other projects.

## [1.0.4] - 22-09-2025
### Added Api key update. Changed the URL, updated the data structure.
- Added enums for pre-selected constructs, metrics.
- Updated the URL for a new url path
- Added an api key field and headers.

## [1.0.5] - 17-12-2025
### Updated the Eye Data Storage script to include a function to get an average calculation from the responses.
### Also I included two extra functions to get the Response Data back based on the pre made Constructs and Metrics.
- These functions have been added to the eye data storage script.

## [1.0.6] - 17-12-2025
### Updated the Eye Data Storage script to include a function to get the latest response list.
### This is helpful for if you have to start and stop the data frequently.
- These functions have been added to the eye data storage script.

## [1.0.7] - 17-12-2025
### Update the Response data string to change from model to predictions.

## [1.1.0] - 17-12-2025
### Major Changes made to allow for multiple responses if you start and stop eye tracking data in one session.
- Added functions 
- GetNewestResponseCollection - This will get the newest response from the response collection.
- GetNewestResponseConstructAverage - This will get the newest response collection from a chosen construct type and return the average score from all models.
- GetNewestResponseMetricAverage - This will get the newest response collection from a chosen metric type and return the average score from all models.
- GetSelectedResponseConstructAverage - This will get a response from the selected response set and search for only a construct type and return the average score from all models for a specific response collection value.
- GetSelectedResponseMetricAverage -This will get a response from the selected response set and search for only a Metric type and return the average score from all models for a specific response collection value.
- GetOverallAverageFromAllResponseSets - This will get the overall average from all response sets for either a metric type or construct type.
## [1.1.1] - 18-12-2025
- Removed unused using statements.
## [1.1.2] - 18-12-2025
- Amended a function for a bug in an if statement.
## [1.2.0] - 05-03-2026
- Added an ELE Logo to the scripts to give it an identity.
- Added DLL so it doesnt show the editor code.
## [1.2.1] - 05-03-2026
- Added a fix for unity 6 as serialized fields are erroring in the new version.
## [1.2.2] - 06-03-2026
- Added an icon for the ELE Prefab to give it an identity.
## [1.3.0] - 09-03-2026
- Added an example script to show how to start and stop eye tracking data.
## [1.4.0] - 09-03-2026
- Added an example Demo Scene along with a few prefabs, materials and models.