﻿using Esri.ArcGISRuntime.Geometry;
using System;
using System.Threading.Tasks;

namespace ExternalNmeaGPS
{
	public class NmeaLocationProvider : Esri.ArcGISRuntime.Location.LocationDataSource
	{
		private NmeaParser.NmeaDevice m_device;
		private double m_Accuracy = 0;
		private double m_altitude = double.NaN;
        private double m_speed = 0;
        private double m_course = 0;

        public NmeaLocationProvider(NmeaParser.NmeaDevice device)
        {
            if (device is null)
                throw new ArgumentNullException(nameof(device));
            this.m_device = device;
            device.MessageReceived += Device_MessageReceived;
        }

		private void Device_MessageReceived(object? sender, NmeaParser.NmeaMessageReceivedEventArgs e)
		{
			var message = e.Message;
			ParseMessage(message);
		}

		public void ParseMessage(NmeaParser.Nmea.NmeaMessage message)
		{
            bool isNewFix = false;
            bool lostFix = false;
            double lat = 0;
            double lon = 0;
			if (message is NmeaParser.Nmea.Garmin.Pgrme)
			{
				m_Accuracy = ((NmeaParser.Nmea.Garmin.Pgrme)message).HorizontalError;
			}
            else if(message is NmeaParser.Nmea.Gst)
            {
                Gst = ((NmeaParser.Nmea.Gst)message);
                m_Accuracy = Math.Sqrt(Gst.SigmaLatitudeError * Gst.SigmaLatitudeError + Gst.SigmaLongitudeError * Gst.SigmaLongitudeError);
            }
            else if(message is NmeaParser.Nmea.Gga)
			{
                Gga = ((NmeaParser.Nmea.Gga)message);
                isNewFix = Gga.Quality != NmeaParser.Nmea.Gga.FixQuality.Invalid;
                lostFix = !isNewFix;
                m_altitude = Gga.Altitude;
                lat = Gga.Latitude;
                lon = Gga.Longitude;
			}
            else if (message is NmeaParser.Nmea.Rmc)
			{
                Rmc = (NmeaParser.Nmea.Rmc)message;
                if (Rmc.Active)
				{
                    isNewFix = true;
                    m_speed = Rmc.Speed;
                    m_course = Rmc.Course;
                    lat = Rmc.Latitude;
                    lon = Rmc.Longitude;
				}
                else lostFix = true;
            }
            else if (message is NmeaParser.Nmea.Gsa)
			{
                Gsa = (NmeaParser.Nmea.Gsa)message;
            }
            if (isNewFix)
				{
                base.UpdateLocation(new Esri.ArcGISRuntime.Location.Location(new MapPoint(lon, lat, m_altitude, SpatialReferences.Wgs84), m_Accuracy, m_speed, m_course, false));
				}
            else if (lostFix)
            {

			}
		}

        protected override Task OnStartAsync()
        {
			if (m_device != null)
            	return this.m_device.OpenAsync();
			else
				return System.Threading.Tasks.Task<bool>.FromResult(true);
        }

        protected override Task OnStopAsync()
        {
            m_Accuracy = double.NaN;
			if(this.m_device != null)
            	return this.m_device.CloseAsync();
			else
				return System.Threading.Tasks.Task<bool>.FromResult(true);
        }

        public NmeaParser.Nmea.Gsa? Gsa { get; private set; }
        public NmeaParser.Nmea.Gga? Gga { get; private set; }
        public NmeaParser.Nmea.Rmc? Rmc { get; private set; }
        public NmeaParser.Nmea.Gst? Gst { get; private set; }
    }
}
