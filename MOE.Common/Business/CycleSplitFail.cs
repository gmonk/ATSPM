﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOE.Common.Business.SplitFail;
using MOE.Common.Business.WCFServiceLibrary;

namespace MOE.Common.Business
{
    public class CycleSplitFail:GreenToGreenCycle
    {

        public enum TerminationType
        {
            MaxOut,
            GapOut,
            ForceOff,
            Unknown
        }
        public TerminationType TerminationEvent { get; private set; }
        public double RedOccupancyTime { get; private set; }
        public double GreenOccupancyTime { get; private set; }
        public double GreenOccupancyPercent { get; private set; }
        public double RedOccupancyPercent { get; private set; }
        public bool IsSplitFail { get; private set; }
        private readonly int _firstSecondsOfRed;

        public CycleSplitFail(DateTime firstGreenEvent, DateTime redEvent, DateTime yellowEvent, DateTime lastGreenEvent, TerminationType terminationType,
             int firstSecondsOfRed):base(firstGreenEvent, redEvent, yellowEvent, lastGreenEvent)
        {
            _firstSecondsOfRed = firstSecondsOfRed;
            TerminationEvent = terminationType;
        }

        public void SetDetectorActivations(List<SplitFailDetectorActivation> detectorActivations)
        {
            var redPeriodToAnalyze = StartTime.AddSeconds(_firstSecondsOfRed);
            var activationsDuringRed = detectorActivations.Where(d => d.DetectorOff > StartTime && d.DetectorOn < redPeriodToAnalyze && d.DetectorOn < RedEvent).ToList();
            if (activationsDuringRed.Count == 0)
            {
                RedOccupancyTime = CheckForDetectorActivationBiggerThanPeriod(StartTime, redPeriodToAnalyze, detectorActivations);
            }
            else
            {
                RedOccupancyTime = GetOccupancy(StartTime, redPeriodToAnalyze, activationsDuringRed);
            }
            var activationsDuringGreen = detectorActivations.Where(d => d.DetectorOff > redPeriodToAnalyze && d.DetectorOn < EndTime).ToList();
            if (activationsDuringGreen.Count == 0)
            {
                GreenOccupancyTime = CheckForDetectorActivationBiggerThanPeriod(StartTime, YellowEvent, detectorActivations);
            }
            else
            {
                GreenOccupancyTime = GetOccupancy(StartTime, YellowEvent, activationsDuringGreen);
            }
            double millisecondsOfRedStart = _firstSecondsOfRed * 1000;
            RedOccupancyPercent = (RedOccupancyTime / millisecondsOfRedStart) * 100;
            GreenOccupancyPercent = (GreenOccupancyTime / TotalGreenTimeMilliseconds) * 100;
            IsSplitFail = (GreenOccupancyPercent > 79 && RedOccupancyPercent > 79);
        }



        private double CheckForDetectorActivationBiggerThanPeriod(DateTime startTime, DateTime endTime, List<SplitFailDetectorActivation> detectorActivations)
        {
            if (detectorActivations.Count(d => d.DetectorOn < startTime && endTime > d.DetectorOff) > 0)
            {
                return (endTime - startTime).Milliseconds;
            }
            return 0;
        }

        private double GetOccupancy(DateTime start, DateTime end, List<SplitFailDetectorActivation> cycleDetectorActivations)
        {
            double occupancy = 0;
            foreach (SplitFailDetectorActivation detectorActivation in cycleDetectorActivations)
            {
                if (detectorActivation.DetectorOn < start && detectorActivation.DetectorOff > end)
                {
                    occupancy += (end - start).TotalMilliseconds;
                }
                else if (detectorActivation.DetectorOn < start)
                {
                    occupancy += (detectorActivation.DetectorOff - start).TotalMilliseconds;
                }
                else if (detectorActivation.DetectorOff > end)
                {
                    occupancy += (end - detectorActivation.DetectorOn).TotalMilliseconds;
                }
                else
                {
                    occupancy += detectorActivation.Duration;
                }
            }
            var totalTime = (end - start).TotalMilliseconds;//Check detector activations for early morning
            return occupancy;
        }

        
    }
}