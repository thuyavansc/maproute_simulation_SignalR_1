﻿using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using maproute_simulation_SignalR_1.Hubs;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using maproute_simulation_SignalR_1.Models;

namespace maproute_simulation_SignalR_1.Hubs
{
    public class MapHub : Hub
    {

        private readonly HttpClient _httpClient;
        private const int DriverToPickupTotalDurationMinutes = 10; // Total duration from driver's current location to pickup location in minutes
        private const int SignalRIntervalSeconds = 2; // Interval between SignalR updates in seconds
        private static RouteDetails routeDetailsVal;

        public MapHub(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"[{DateTime.Now}] Device connected: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"[{DateTime.Now}] Device disconnected: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SimulateDriver(string pickupLatitude, string pickupLongitude, string dropoffLatitude, string dropoffLongitude)
        {
            try
            {
                Console.WriteLine("Received request to simulate driver.");
                Console.WriteLine($"Pickup Location: ({pickupLatitude}, {pickupLongitude})");
                Console.WriteLine($"Dropoff Location: ({dropoffLatitude}, {dropoffLongitude})");

                // Convert latitude and longitude strings to double
                double pickupLat = double.Parse(pickupLatitude);
                double pickupLon = double.Parse(pickupLongitude);

                // Step 1: Find a random location near the pickup location as Driver Current location
                GeoCoordinates driverLocation = await GetRandomLocation(pickupLat, pickupLon);
                Console.WriteLine($"Generated driver location: ({driverLocation.Latitude}, {driverLocation.Longitude})");

                // Step 2: Retrieve route coordinates from the pickup location to the random location
                RouteDetails routeDetails = await GetRouteDetails(driverLocation.Latitude, driverLocation.Longitude, pickupLat, pickupLon);
                Console.WriteLine($"Retrieved route details: {JsonConvert.SerializeObject(routeDetails)}");
                routeDetailsVal = routeDetails;
                Console.WriteLine($"AcceptRide 0 : Retrieved route details: {JsonConvert.SerializeObject(routeDetails)}");

                if (routeDetails != null)
                {
                    routeDetailsVal = routeDetails;
                    Console.WriteLine($"AcceptRide 1 : Retrieved route details: {JsonConvert.SerializeObject(routeDetails)}");
                    Console.WriteLine($"AcceptRide 2 : Retrieved route details: {JsonConvert.SerializeObject(routeDetailsVal)}");
                }
                else
                {
                    Console.WriteLine("Error: Retrieved route details is null.");
                }


                // Step 3: Calculate interpolated driver coordinates along the route
                //await SendDriverLocationUpdates(routeDetails, DriverToPickupTotalDurationMinutes, SignalRIntervalSeconds);
                await NotifyNewRide(pickupLatitude, pickupLongitude, dropoffLatitude, dropoffLongitude);

            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine($"Error: {ex.Message}");
            }
        }


        private async Task SendDriverLocationUpdates0(RouteDetails routeDetails, int totalDurationMinutes, int signalRIntervalSeconds)
        {
            Console.WriteLine("Entered a Method: SendDriverLocationUpdates");

            if (routeDetails.Features != null && routeDetails.Features.Length > 0 && routeDetails.Features[0].Geometry != null && routeDetails.Features[0].Geometry.Coordinates != null)
            {
                double[][][] coordinates = routeDetails.Features[0].Geometry.Coordinates;
                int numSteps = coordinates.Length;
                int totalSteps = totalDurationMinutes * 60 / signalRIntervalSeconds;

                for (int i = 0; i < totalSteps; i++)
                {
                    int index = i * numSteps / totalSteps; // Calculate the index based on the step

                    // Check if the calculated index is within bounds
                    if (index >= 0 && index < numSteps)
                    {
                        double latitude = coordinates[index][0][1]; // Latitude is at index 1
                        double longitude = coordinates[index][0][0]; // Longitude is at index 0

                        Console.WriteLine("Latitude: " + latitude + ", Longitude: " + longitude);

                        await Clients.All.SendAsync("DriverLocationUpdate", latitude, longitude);

                        await Task.Delay(signalRIntervalSeconds * 1000);
                    }
                    else
                    {
                        Console.WriteLine("Invalid index calculated: " + index);
                    }
                }
            }
            else
            {
                Console.WriteLine("Route details or coordinates are null.");
            }
        }


        private async Task SendDriverLocationUpdates1(RouteDetails routeDetails, int totalDurationMinutes, int signalRIntervalSeconds)
        {
            Console.WriteLine("Entered a Method: SendDriverLocationUpdates");

            if (routeDetails.Features != null && routeDetails.Features.Length > 0 && routeDetails.Features[0].Geometry != null && routeDetails.Features[0].Geometry.Coordinates != null)
            {
                double[][][] coordinates = routeDetails.Features[0].Geometry.Coordinates;
                int numSteps = coordinates[0].Length;

                for (int i = 0; i < numSteps; i++)
                {
                    double latitude = coordinates[0][i][1]; // Latitude is at index 1
                    double longitude = coordinates[0][i][0]; // Longitude is at index 0

                    Console.WriteLine("Latitude: " + latitude + ", Longitude: " + longitude);

                    await Clients.All.SendAsync("DriverLocationUpdate", latitude, longitude);

                    await Task.Delay(signalRIntervalSeconds * 1000);
                }
            }
            else
            {
                Console.WriteLine("Route details or coordinates are null.");
            }
        }

        private async Task SendDriverLocationUpdates(RouteDetails routeDetails, int totalDurationMinutes, int signalRIntervalSeconds)
        {
            Console.WriteLine("Entered a Method: SendDriverLocationUpdates");

            if (routeDetails != null && routeDetails.Features != null && routeDetails.Features.Length > 0 && routeDetails.Features[0].Geometry != null && routeDetails.Features[0].Geometry.Coordinates != null)
            {
                double[][][] coordinates = routeDetails.Features[0].Geometry.Coordinates;
                int numSteps = coordinates[0].Length;

                for (int i = 0; i < numSteps; i++)
                {
                    double latitude = coordinates[0][i][1]; // Latitude is at index 1
                    double longitude = coordinates[0][i][0]; // Longitude is at index 0

                    Console.WriteLine("Latitude: " + latitude + ", Longitude: " + longitude);

                    await Clients.All.SendAsync("DriverLocationUpdate", latitude, longitude);

                    await Task.Delay(signalRIntervalSeconds * 1000);
                }
            }
            else
            {
                Console.WriteLine("Route details or coordinates are null.");
            }
        }



        private async Task<GeoCoordinates> GetRandomLocation(double pickupLatitude, double pickupLongitude)
        {
            var random = new Random();

            // Generate random distance between 500 meters and 1 km (in meters)
            var distance = random.Next(500, 1000);
            Console.WriteLine("Generate random distance : " + $"Generated random distance: {distance} meters");

            // Generate random angle (in radians)
            var angle = random.NextDouble() * 2 * Math.PI;
            Console.WriteLine($"Generated random angle: {angle} radians");

            // Calculate latitude and longitude offsets using trigonometry
            var latOffset = distance * Math.Cos(angle) / 111000; // 1 degree latitude = 111 km
            var lonOffset = distance * Math.Sin(angle) / (111000 * Math.Cos(pickupLatitude * Math.PI / 180)); // 1 degree longitude = 111 km * cos(latitude)
            Console.WriteLine($"Latitude offset: {latOffset}, Longitude offset: {lonOffset}");

            // Calculate driver's latitude and longitude
            var driverLatitude = pickupLatitude + latOffset;
            var driverLongitude = pickupLongitude + lonOffset;

            return new GeoCoordinates { Latitude = driverLatitude, Longitude = driverLongitude };
        }

        private async Task<RouteDetails> GetRouteDetails(double pickupLatitude, double pickupLongitude, double driverLatitude, double driverLongitude)
        {
            // Call the routing API to get route details
            string apiUrl = $"https://api.geoapify.com/v1/routing?waypoints={pickupLatitude},{pickupLongitude}%7C{driverLatitude},{driverLongitude}&mode=drive&apiKey=a2324ce78a1a47e581fe3bad094fdeb6";

            // Send HTTP GET request to the API
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            Console.WriteLine($"Sending request to API: {apiUrl}");

            // Check if the response is successful
            if (response.IsSuccessStatusCode)
            {
                // Read the response content
                string json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Received API response: {json}");

                // Deserialize JSON to RouteDetails object
                RouteDetails routeDetails = JsonConvert.DeserializeObject<RouteDetails>(json);
                return routeDetails;
            }
            else
            {
                throw new Exception($"Failed to retrieve route details. Status code: {response.StatusCode}");
            }
        }

        public async Task NotifyNewRide(string pickupLatitude, string pickupLongitude, string dropoffLatitude, string dropoffLongitude)
        {
            Console.WriteLine($"[{DateTime.Now}] New ride requested: Pickup ({pickupLatitude}, {pickupLongitude}), Dropoff ({dropoffLatitude}, {dropoffLongitude})");
            await Clients.All.SendAsync("NewRideNotification", pickupLatitude, pickupLongitude, dropoffLatitude, dropoffLongitude);
        }

        public async Task AcceptRide(string pickupLatitude, string pickupLongitude, string dropoffLatitude, string dropoffLongitude)
        {
            Console.WriteLine($"[{DateTime.Now}]  Ride AcceptRide: Pickup ({pickupLatitude}, {pickupLongitude}), Dropoff ({dropoffLatitude}, {dropoffLongitude})");
            // Place any necessary logic here to process the accepted ride
            await Clients.All.SendAsync("RideAccepted", pickupLatitude, pickupLongitude, dropoffLatitude, dropoffLongitude);
            Console.WriteLine($"AcceptRide : Retrieved route details: {JsonConvert.SerializeObject(routeDetailsVal)}");
            await SendDriverLocationUpdates(routeDetailsVal, DriverToPickupTotalDurationMinutes, SignalRIntervalSeconds);

        }

        public async Task SayHi(string pickupLatitude)
        {
            Console.WriteLine($"[{DateTime.Now}]  SayHi: Pickup ({pickupLatitude})");
            // Place any necessary logic here to process the accepted ride
            await Clients.All.SendAsync("HiReceived", pickupLatitude);

        }

    }

    public class GeoCoordinates
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class RouteDetails
    {
        public Feature[] Features { get; set; }
    }

    public class Feature
    {
        public Geometry Geometry { get; set; }
    }

    public class Geometry
    {
        public string Type { get; set; }
        public double[][][] Coordinates { get; set; }
    }
}