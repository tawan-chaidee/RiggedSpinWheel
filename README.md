# RiggedSpinWheel

## Project Description

A web-based “Wheel of Death” game where the room owner can share a link for others to watch the spin.  Owner can secretly rig the result without the observers knowing. Built with real-time updates.

Inspired by a group presentation scenario where I wanted to avoid being selected as the presenter.

## Tech Stack

- **Backend:** C# .NET with SignalR WebSocket for handling concurrent observers and multiple rooms.  
- **Frontend:** Simple HTML, JavaScript, and CSS for simplicity and speed.

## Deployment

To deploy using Heroku, simply install the Heroku CLI, log in, and then use the following commands:

```bash
git clone https://github.com/tawan-chaidee/RiggedSpinWheel.git
cd RiggedSpinWheel
heroku create spinwheel-production
git push heroku main
heroku open
```

## Run locally

```bash
git clone https://github.com/tawan-chaidee/RiggedSpinWheel.git
cd RiggedSpinWheel
dotnet run
```
