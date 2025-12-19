namespace ACARSPlugin.Configuration;

// Left-click to use the popup
// Middle-click to clear the value
// Right-click to enter text manually
// If no popup is available, use manual text entry

public enum UplinkMessageParameterType
{
    Altimeter,
    AtisCode,
    Code,
    Degree, // 3 digitsd of free text followed by M or T
    Direction, // L, R, LR, N, S, E, W, NE, NW, SE, SW
    DistanceOFfset,
    FacilityDesignation,
    FreeText,
    Frequency,
    LegType,
    Level, // Altitude popup - Left click for CFL style menu, right click for text. Text is three digit altitude. FL and A inserted automatically based on transition level.
    Position, // 9 positions (fixes in route) following the current position
    PreDepartureClerance,
    ProcedureName, // Intermediate window. Arrival / Approach / Departure popup -> NAME TYPE TRANSITION window.
    RouteClearance, // Screw that...
    Speed, // Speed popup. Only Mach and TAS are available via the popup. Free text = Prefix + 3 digits. Prefix is either M (Mach), N (TAS), I (IAS), or G (GS)
    Time, // Time popup
    ToFrom, // TO or FROM
    UnitName, // Offline defined unit names (AUCKLAND CTR, BRISBANE CTR, MELBOURNE CTR, NADI CTR, etc.)
    VerticalRate
}

public class Testing
{
  public static string PermanentMessageClassName = "PERMANENT";
  
  public static UplinkMessageTemplates UplinkMessageTemplates => new UplinkMessageTemplates
  {
    Messages = new Dictionary<string, UplinkMessageTemplate[]>
    {
      [PermanentMessageClassName] = [
        new() { Template = "CLIMB TO AND MAINTAIN [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "DESCEND TO AND MAINTAIN [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "PROCEED DIRECT TO [POSITION]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "MAINTAIN [SPEED] OR GREATER", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "MAINTAIN [SPEED] OR LESS", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CONTACT [UNIT] [FREQUENCY]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "REQUEST RECEIVED RESPONSE WILL BE VIA VOICE", ResponseType = UplinkResponseType.Roger }
      ],
      ["Level"] = [
        new() { Template = "WHEN CAN YOU ACCEPT [LEVEL]", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CAN YOU ACCEPT [LEVEL] AT [POSITION]", ResponseType = UplinkResponseType.AffirmativeNegative },
        new() { Template = "CAN YOU ACCEPT [LEVEL] AT [TIME]", ResponseType = UplinkResponseType.AffirmativeNegative },
        new() { Template = "MAINTAIN [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CLIMB TO AND MAINTAIN [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CLIMB VIA SID TO [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [TIME] CLIMB TO AND MAINTAIN [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [POSITION] CLIMB TO AND MAINTAIN [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "DESCEND TO AND MAINTAIN [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "DESCEND VIA STAR TO [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [TIME] DESCEND TO AND MAINTAIN [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [POSITION] DESCEND TO AND MAINTAIN [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CLIMB TO REACH [LEVEL] BY [TIME]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CLIMB TO REACH [LEVEL] BY [POSITION]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "DESCEND TO REACH [LEVEL] BY [TIME]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "DESCEND TO REACH [LEVEL] BY [POSITION]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "REACH [LEVEL] BY [TIME]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "REACH [LEVEL] BY [POSITION]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "MAINTAIN BLOCK [LEVEL] TO [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CLIMB TO AND MAINTAIN BLOCK [LEVEL] TO [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "DESCEND TO AND MAINTAIN BLOCK [LEVEL] TO [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "LEAVE CONTROL AREA DESCENDING", ResponseType = UplinkResponseType.Roger },
        new() { Template = "CRUISE CLIMB NOT AVAILABLE IN AUSTRALIAN ADMINISTERED AIRSPACE", ResponseType = UplinkResponseType.Roger },
        new() { Template = "CRUISE [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CRUISE CLIMB TO [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CRUISE CLIMB ABOVE [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "EXPEDITE CLIMB TO [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "EXPEDITE DESCENT TO [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "IMMEDIATELY CLIMB TO [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "IMMEDIATELY DESCEND TO [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "IMMEDIATELY STOP CLIMB AT [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "IMMEDIATELY STOP DESCENT AT [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CLIMB AT [RATE] MINIMUM", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CLIMB AT [RATE] MAXIMUM", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "DESCEND AT [RATE] MINIMUM", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "DESCEND AT [RATE] MAXIMUM", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "EXPECT [LEVEL]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "EXPECT CLIMB AT [TIME]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "EXPECT CLIMB AT [POSITION]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "EXPECT DESCENT AT [TIME]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "EXPECT DESCENT AT [POSITION]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "EXPECT CRUISE CLIMB AT [TIME]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "EXPECT CRUISE CLIMB AT [POSITION]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "AT [TIME] EXPECT CLIMB TO [LEVEL]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "AT [POSITION] EXPECT CLIMB TO [LEVEL]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "AT [TIME] EXPECT DESCENT TO [LEVEL]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "AT [POSITION] EXPECT DESCENT TO [LEVEL]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "AT [TIME] EXPECT CRUISE CLIMB TO [LEVEL]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "AT [POSITION] EXPECT CRUISE CLIMB TO [LEVEL]", ResponseType = UplinkResponseType.Roger }
      ],
      ["Cross"] = [
        new() { Template = "CROSS [POSITION] AT [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CROSS [POSITION] AT OR ABOVE [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CROSS [POSITION] AT OR BELOW [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CROSS [POSITION] AT AND MAINTAIN [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CROSS [POSITION] BETWEEN [LEVEL] AND [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CROSS [POSITION] AT [TIME]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CROSS [POSITION] AT OR BEFORE [TIME]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CROSS [POSITION] AT OR AFTER [TIME]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CROSS [POSITION] BETWEEN [TIME] AND [TIME]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CROSS [POSITION] AT [SPEED]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CROSS [POSITION] AT OR LESS THAN [SPEED]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CROSS [POSITION] AT OR GREATER THAN [SPEED]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CROSS [POSITION] AT [TIME] AT [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CROSS [POSITION] AT OR BEFORE [TIME] AT [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CROSS [POSITION] AT OR AFTER [TIME] AT [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CROSS [POSITION] AT AND MAINTAIN [LEVEL] AT [SPEED]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [TIME] CROSS [POSITION] AT AND MAINTAIN [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [TIME] CROSS [POSITION] AT AND MAINTAIN [LEVEL] AT [SPEED]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "EXPECT TO CROSS [POSITION] AT [LEVEL]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "EXPECT TO CROSS [POSITION] AT OR ABOVE [LEVEL]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "EXPECT TO CROSS [POSITION] AT OR BELOW [LEVEL]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "EXPECT TO CROSS [POSITION] AT AND MAINTAIN [LEVEL]", ResponseType = UplinkResponseType.Roger }
      ],
      ["Divert"] = [
        new() { Template = "WHEN CAN YOU ACCEPT [DISTANCE] [DIRECTION] OFFSET", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "OFFSET [DISTANCE] [DIRECTION] OF ROUTE", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [POSITION] OFFSET [DISTANCE] [DIRECTION] OF ROUTE", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [TIME] OFFSET [DISTANCE] [DIRECTION] OF ROUTE", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "PROCEED BACK ON ROUTE", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "REJOIN ROUTE BY [POSITION]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "REJOIN ROUTE BY [TIME]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "RESUME OWN NAVIGATION", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "EXPECT BACK ON ROUTE BY [POSITION]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "EXPECT BACK ON ROUTE BY [TIME]", ResponseType = UplinkResponseType.Roger }
      ],
      ["Route"] = [
        new() { Template = "PROCEED DIRECT TO [POSITION]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "WHEN ABLE PROCEED DIRECT TO [POSITION]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [TIME] PROCEED DIRECT TO [POSITION]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [POSITION] PROCEED DIRECT TO [POSITION]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [LEVEL] PROCEED DIRECT TO [POSITION]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CLEARED TO [POSITION] VIA [ROUTE]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CLEARED [ROUTE]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CLEARED [STAR] [TEXT]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "CLEARED TO DEVIATE UP TO [DISTANCE] [DIRECTION] OF ROUTE", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [POSITION] CLEARED [ROUTE]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [POSITION] CLEARED [STAR] [TEXT]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "HOLD AT [POSITION] MAINTAIN [LEVEL] INBOUND TRACK [DEGREES] [DIRECTION] TURN LEG TIME [TIME]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "HOLD AT [POSITION] AS PUBLISHED MAINTAIN [LEVEL]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "TURN [DIRECTION] HEADING [DEGREES]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "TURN [DIRECTION] GROUND TRACK [DEGREES]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "FLY PRESENT HEADING", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [POSITION] FLY HEADING [DEGREES]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "IMMEDIATELY TURN [DIRECTION] HEADING [DEGREES]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "EXPECT [ROUTE]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "AT [POSITION] EXPECT [ROUTE]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "EXPECT DIRECT TO [POSITION]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "AT [POSITION] EXPECT DIRECT TO [POSITION]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "AT [TIME] EXPECT DIRECT TO [POSITION]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "AT [LEVEL] EXPECT DIRECT TO [POSITION]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "EXPECT FURTHER CLEARANCE AT [TIME]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "EXPECT [STAR] [TEXT]", ResponseType = UplinkResponseType.Roger }
      ],
      ["Speed"] = [
        new() { Template = "MAINTAIN [SPEED]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "MAINTAIN PRESENT SPEED", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "MAINTAIN [SPEED] OR GREATER", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "MAINTAIN [SPEED] OR LESS", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "MAINTAIN [SPEED] TO [SPEED]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "INCREASE SPEED TO [SPEED]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "INCREASE SPEED TO [SPEED] OR GREATER", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "REDUCE SPEED TO [SPEED]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "REDUCE SPEED TO [SPEED] OR LESS", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "DO NOT EXCEED [SPEED]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "RESUME NORMAL SPEED", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "NO SPEED RESTRICTION", ResponseType = UplinkResponseType.Roger },
        new() { Template = "AT [TIME] EXPECT [SPEED]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "AT [POSITION] EXPECT [SPEED]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "AT [LEVEL] EXPECT [SPEED]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "AT [TIME] EXPECT [SPEED] TO [SPEED]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "AT [POSITION] EXPECT [SPEED] TO [SPEED]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "AT [LEVEL] EXPECT [SPEED] TO [SPEED]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "WHEN CAN YOU ACCEPT [SPEED]", ResponseType = UplinkResponseType.NoResponse }
      ],
      ["Comms"] = [
        new() { Template = "CONTACT [UNIT] [FREQUENCY]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [POSITION] CONTACT [UNIT] [FREQUENCY]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [TIME] CONTACT [UNIT] [FREQUENCY]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "MONITOR [UNIT] [FREQUENCY]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [POSITION] MONITOR [UNIT] [FREQUENCY]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT [TIME] MONITOR [UNIT] [FREQUENCY]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "SQUAWK [SSR]", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "STOP SQUAWK", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "SQUAWK ALTITUDE", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "STOP SQUAWK ALTITUDE", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "SQUAWK IDENT", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "ALTIMETER [QNH]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "IDENTIFICATION", ResponseType = UplinkResponseType.Roger },
        new() { Template = "IDENTIFIED", ResponseType = UplinkResponseType.Roger },
        new() { Template = "IDENTIFICATION TERMINATED", ResponseType = UplinkResponseType.Roger },
        new() { Template = "TRANSMIT ADS-B IDENT", ResponseType = UplinkResponseType.Roger },
        new() { Template = "CONFIRM ADS-C EMERGENCY", ResponseType = UplinkResponseType.Roger },
        new() { Template = "CHECK STUCK MICROPHONE [FREQUENCY]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "ATIS [CODE]", ResponseType = UplinkResponseType.Roger }
      ],
      ["Report"] = [
        new() { Template = "REPORT BACK ON ROUTE", ResponseType = UplinkResponseType.Roger },
        new() { Template = "REPORT LEAVING [LEVEL]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "REPORT LEVEL [LEVEL]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "REPORT REACHING [LEVEL]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "REPORT REACHING BLOCK [LEVEL] TO [LEVEL]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "REPORT PASSING [POSITION]", ResponseType = UplinkResponseType.Roger },
        new() { Template = "REPORT DISTANCE [TO/FROM] [POSITION]", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "REPORT REMAINING FUEL AND SOULS ON BOARD", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CONFIRM POSITION", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CONFIRM ALTITUDE", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CONFIRM SPEED", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CONFIRM ASSIGNED ALTITUDE", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CONFIRM ASSIGNED SPEED", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CONFIRM ASSIGNED ROUTE", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CONFIRM TIME OVER REPORTED WAYPOINT", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CONFIRM REPORTED WAYPOINT", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CONFIRM NEXT WAYPOINT", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CONFIRM NEXT WAYPOINT ETA", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CONFIRM ENSUING WAYPOINT", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CONFIRM REQUEST", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CONFIRM SQUAWK", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CONFIRM HEADING", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CONFIRM GROUND TRACK", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "CONFIRM ATIS CODE", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "REQUEST POSITION REPORT", ResponseType = UplinkResponseType.NoResponse }
      ],
      ["Misc"] = [
        new() { Template = "REQUEST RECEIVED RESPONSE WILL BE VIA VOICE", ResponseType = UplinkResponseType.Roger },
        new() { Template = "WHEN READY", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "THEN", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "DUE TO TRAFFIC", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "DUE TO AIRSPACE RESTRICTION", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "DISREGARD", ResponseType = UplinkResponseType.Roger },
        new() { Template = "MAINTAIN OWN SEPARATION AND VMC", ResponseType = UplinkResponseType.WilcoUnable },
        new() { Template = "AT PILOTS DISCRETION", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "STANDBY", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "UNABLE", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "ROGER", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "AFFIRM", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "NEGATIVE", ResponseType = UplinkResponseType.NoResponse },
        new() { Template = "ROGER MAYDAY", ResponseType = UplinkResponseType.Roger },
        new() { Template = "ROGER PAN", ResponseType = UplinkResponseType.Roger }
      ]
    }
  };
}

/*
<?xml version="1.0" encoding="utf-8" ?>
<Messages>
  <Category name="QUICK_OPTIONS">
    <Message Response="WU">CLIMB TO AND MAINTAIN [LEVEL]</Message>
    <Message Response="WU">DESCEND TO AND MAINTAIN [LEVEL]</Message>
    <Message Response="WU">PROCEED DIRECT TO [POSITION]</Message>
    <Message Response="WU">MAINTAIN [SPEED] OR GREATER</Message>
    <Message Response="WU">MAINTAIN [SPEED] OR LESS</Message>
    <Message Response="WU">CONTACT [UNIT] [FREQUENCY]</Message>
    <Message Response="R">REQUEST RECEIVED RESPONSE WILL BE VIA VOICE</Message>
  </Category>
  <Category name="Level">
    <Message Response="NE">WHEN CAN YOU ACCEPT [LEVEL]</Message>
    <Message Response="AN">CAN YOU ACCEPT [LEVEL] AT [POSITION]</Message>
    <Message Response="AN">CAN YOU ACCEPT [LEVEL] AT [TIME]</Message>
    <Message Response="WU">MAINTAIN [LEVEL]</Message>
    <Message Response="WU">CLIMB TO AND MAINTAIN [LEVEL]</Message>
	<Message Response="WU">CLIMB VIA SID TO [LEVEL]</Message>
    <Message Response="WU">AT [TIME] CLIMB TO AND MAINTAIN [LEVEL]</Message>
    <Message Response="WU">AT [POSITION] CLIMB TO AND MAINTAIN [LEVEL]</Message>
    <Message Response="WU">DESCEND TO AND MAINTAIN [LEVEL]</Message>
	<Message Response="WU">DESCEND VIA STAR TO [LEVEL]</Message>
    <Message Response="WU">AT [TIME] DESCEND TO AND MAINTAIN [LEVEL]</Message>
    <Message Response="WU">AT [POSITION] DESCEND TO AND MAINTAIN [LEVEL]</Message>
    <Message Response="WU">CLIMB TO REACH [LEVEL] BY [TIME]</Message>
    <Message Response="WU">CLIMB TO REACH [LEVEL] BY [POSITION]</Message>
    <Message Response="WU">DESCEND TO REACH [LEVEL] BY [TIME]</Message>
    <Message Response="WU">DESCEND TO REACH [LEVEL] BY [POSITION]</Message>
    <Message Response="R">REACH [LEVEL] BY [TIME]</Message>
    <Message Response="R">REACH [LEVEL] BY [POSITION]</Message>
    <Message Response="WU">MAINTAIN BLOCK [LEVEL] TO [LEVEL]</Message>
    <Message Response="WU">CLIMB TO AND MAINTAIN BLOCK [LEVEL] TO [LEVEL]</Message>
    <Message Response="WU">DESCEND TO AND MAINTAIN BLOCK [LEVEL] TO [LEVEL]</Message>
    <Message Response="R">LEAVE CONTROL AREA DESCENDING</Message>
    <Message Response="R">CRUISE CLIMB NOT AVAILABLE IN AUSTRALIAN ADMINISTERED AIRSPACE</Message>
    <Message Response="WU">CRUISE [LEVEL]</Message>
    <Message Response="WU">CRUISE CLIMB TO [LEVEL]</Message>
    <Message Response="WU">CRUISE CLIMB ABOVE [LEVEL]</Message>
    <Message Response="WU">EXPEDITE CLIMB TO [LEVEL]</Message>
    <Message Response="WU">EXPEDITE DESCENT TO [LEVEL]</Message>
    <Message Response="WU">IMMEDIATELY CLIMB TO [LEVEL]</Message>
    <Message Response="WU">IMMEDIATELY DESCEND TO [LEVEL]</Message>
    <Message Response="WU">IMMEDIATELY STOP CLIMB AT [LEVEL]</Message>
    <Message Response="WU">IMMEDIATELY STOP DESCENT AT [LEVEL]</Message>
    <Message Response="WU">CLIMB AT [RATE] MINIMUM</Message>
    <Message Response="WU">CLIMB AT [RATE] MAXIMUM</Message>
    <Message Response="WU">DESCEND AT [RATE] MINIMUM</Message>
    <Message Response="WU">DESCEND AT [RATE] MAXIMUM</Message>
    <Message Response="R">EXPECT [LEVEL]</Message>
    <Message Response="R">EXPECT CLIMB AT [TIME]</Message>
    <Message Response="R">EXPECT CLIMB AT [POSITION]</Message>
    <Message Response="R">EXPECT DESCENT AT [TIME]</Message>
    <Message Response="R">EXPECT DESCENT AT [POSITION]</Message>
    <Message Response="R">EXPECT CRUISE CLIMB AT [TIME]</Message>
    <Message Response="R">EXPECT CRUISE CLIMB AT [POSITION]</Message>
    <Message Response="R">AT [TIME] EXPECT CLIMB TO [LEVEL]</Message>
    <Message Response="R">AT [POSITION] EXPECT CLIMB TO [LEVEL]</Message>
    <Message Response="R">AT [TIME] EXPECT DESCENT TO [LEVEL]</Message>
    <Message Response="R">AT [POSITION] EXPECT DESCENT TO [LEVEL]</Message>
    <Message Response="R">AT [TIME] EXPECT CRUISE CLIMB TO [LEVEL]</Message>
    <Message Response="R">AT [POSITION] EXPECT CRUISE CLIMB TO [LEVEL]</Message>
  </Category>
  <Category name="Cross">
    <Message Response="WU">CROSS [POSITION] AT [LEVEL]</Message>
    <Message Response="WU">CROSS [POSITION] AT OR ABOVE [LEVEL]</Message>
    <Message Response="WU">CROSS [POSITION] AT OR BELOW [LEVEL]</Message>
    <Message Response="WU">CROSS [POSITION] AT AND MAINTAIN [LEVEL]</Message>
    <Message Response="WU">CROSS [POSITION] BETWEEN [LEVEL] AND [LEVEL]</Message>
    <Message Response="WU">CROSS [POSITION] AT [TIME]</Message>
    <Message Response="WU">CROSS [POSITION] AT OR BEFORE [TIME]</Message>
    <Message Response="WU">CROSS [POSITION] AT OR AFTER [TIME]</Message>
    <Message Response="WU">CROSS [POSITION] BETWEEN [TIME] AND [TIME]</Message>
    <Message Response="WU">CROSS [POSITION] AT [SPEED]</Message>
    <Message Response="WU">CROSS [POSITION] AT OR LESS THAN [SPEED]</Message>
    <Message Response="WU">CROSS [POSITION] AT OR GREATER THAN [SPEED]</Message>
    <Message Response="WU">CROSS [POSITION] AT [TIME] AT [LEVEL]</Message>
    <Message Response="WU">CROSS [POSITION] AT OR BEFORE [TIME] AT [LEVEL]</Message>
    <Message Response="WU">CROSS [POSITION] AT OR AFTER [TIME] AT [LEVEL]</Message>
    <Message Response="WU">CROSS [POSITION] AT AND MAINTAIN [LEVEL] AT [SPEED]</Message>
    <Message Response="WU">AT [TIME] CROSS [POSITION] AT AND MAINTAIN [LEVEL]</Message>
    <Message Response="WU">AT [TIME] CROSS [POSITION] AT AND MAINTAIN [LEVEL] AT [SPEED]</Message>
    <Message Response="R">EXPECT TO CROSS [POSITION] AT [LEVEL]</Message>
    <Message Response="R">EXPECT TO CROSS [POSITION] AT OR ABOVE [LEVEL]</Message>
    <Message Response="R">EXPECT TO CROSS [POSITION] AT OR BELOW [LEVEL]</Message>
    <Message Response="R">EXPECT TO CROSS [POSITION] AT AND MAINTAIN [LEVEL]</Message>
  </Category>
  <Category name="Divert">
    <Message Response="NE">WHEN CAN YOU ACCEPT [DISTANCE] [DIRECTION] OFFSET</Message>
    <Message Response="WU">OFFSET [DISTANCE] [DIRECTION] OF ROUTE</Message>
    <Message Response="WU">AT [POSITION] OFFSET [DISTANCE] [DIRECTION] OF ROUTE</Message>
    <Message Response="WU">AT [TIME] OFFSET [DISTANCE] [DIRECTION] OF ROUTE</Message>
    <Message Response="WU">PROCEED BACK ON ROUTE</Message>
    <Message Response="WU">REJOIN ROUTE BY [POSITION]</Message>
    <Message Response="WU">REJOIN ROUTE BY [TIME]</Message>
    <Message Response="WU">RESUME OWN NAVIGATION</Message>
    <Message Response="R">EXPECT BACK ON ROUTE BY [POSITION]</Message>
    <Message Response="R">EXPECT BACK ON ROUTE BY [TIME]</Message>
  </Category>
  <Category name="Route">
    <Message Response="WU">PROCEED DIRECT TO [POSITION]</Message>
    <Message Response="WU">WHEN ABLE PROCEED DIRECT TO [POSITION]</Message>
    <Message Response="WU">AT [TIME] PROCEED DIRECT TO [POSITION]</Message>
    <Message Response="WU">AT [POSITION] PROCEED DIRECT TO [POSITION]</Message>
    <Message Response="WU">AT [LEVEL] PROCEED DIRECT TO [POSITION]</Message>
    <Message Response="WU">CLEARED TO [POSITION] VIA [ROUTE]</Message>
    <Message Response="WU">CLEARED [ROUTE]</Message>
    <Message Response="WU">CLEARED [STAR] [TEXT]</Message>
    <Message Response="WU">CLEARED TO DEVIATE UP TO [DISTANCE] [DIRECTION] OF ROUTE</Message>
    <Message Response="WU">AT [POSITION] CLEARED [ROUTE]</Message>
    <Message Response="WU">AT [POSITION] CLEARED [STAR] [TEXT]</Message>
    <Message Response="WU">HOLD AT [POSITION] MAINTAIN [LEVEL] INBOUND TRACK [DEGREES] [DIRECTION] TURN LEG TIME [TIME]</Message>
    <Message Response="WU">HOLD AT [POSITION] AS PUBLISHED MAINTAIN [LEVEL]</Message>
    <Message Response="WU">TURN [DIRECTION] HEADING [DEGREES]</Message>
    <Message Response="WU">TURN [DIRECTION] GROUND TRACK [DEGREES]</Message>
    <Message Response="WU">FLY PRESENT HEADING</Message>
    <Message Response="WU">AT [POSITION] FLY HEADING [DEGREES]</Message>
    <Message Response="WU">IMMEDIATELY TURN [DIRECTION] HEADING [DEGREES]</Message>
    <Message Response="R">EXPECT [ROUTE]</Message>
    <Message Response="R">AT [POSITION] EXPECT [ROUTE]</Message>
    <Message Response="R">EXPECT DIRECT TO [POSITION]</Message>
    <Message Response="R">AT [POSITION] EXPECT DIRECT TO [POSITION]</Message>
    <Message Response="R">AT [TIME] EXPECT DIRECT TO [POSITION]</Message>
    <Message Response="R">AT [LEVEL] EXPECT DIRECT TO [POSITION]</Message>
    <Message Response="R">EXPECT FURTHER CLEARANCE AT [TIME]</Message>
    <Message Response="R">EXPECT [STAR] [TEXT]</Message>
  </Category>

  <Category name="PDC" AutoloadElements="true" SendViaTM="true">
    <Message Response="NE">PDC [TIMESTAMP]</Message>
    <Message Response="NE">[CALLSIGN] [ATYPE] [ADEP] [ETD]</Message>
    <Message Response="NE">CLEARED TO [ADES] VIA [TEXT] [SID] DEP [TEXT]</Message>
    <Message Response="NE">ROUTE [ROUTE]</Message>
    <Message Response="NE">CLIMB VIA SID TO [LEVEL]</Message>
    <Message Response="NE">DEP FREQ [FREQUENCY]</Message>
    <Message Response="NE">SQUAWK [SSR]</Message>
	<Message Response="NE">ONLY READBACK SID, SQUAWK CODE, AND BAY NO. ON [FREQUENCY]</Message>
  </Category>
  <Category name="Speed">
    <Message Response="WU">MAINTAIN [SPEED]</Message>
    <Message Response="WU">MAINTAIN PRESENT SPEED</Message>
    <Message Response="WU">MAINTAIN [SPEED] OR GREATER</Message>
    <Message Response="WU">MAINTAIN [SPEED] OR LESS</Message>
    <Message Response="WU">MAINTAIN [SPEED] TO [SPEED]</Message>
    <Message Response="WU">INCREASE SPEED TO [SPEED]</Message>
    <Message Response="WU">INCREASE SPEED TO [SPEED] OR GREATER</Message>
    <Message Response="WU">REDUCE SPEED TO [SPEED]</Message>
    <Message Response="WU">REDUCE SPEED TO [SPEED] OR LESS</Message>
    <Message Response="WU">DO NOT EXCEED [SPEED]</Message>
    <Message Response="WU">RESUME NORMAL SPEED</Message>
    <Message Response="R">NO SPEED RESTRICTION</Message>
    <Message Response="R">AT [TIME] EXPECT [SPEED]</Message>
    <Message Response="R">AT [POSITION] EXPECT [SPEED]</Message>
    <Message Response="R">AT [LEVEL] EXPECT [SPEED]</Message>
    <Message Response="R">AT [TIME] EXPECT [SPEED] TO [SPEED]</Message>
    <Message Response="R">AT [POSITION] EXPECT [SPEED] TO [SPEED]</Message>
    <Message Response="R">AT [LEVEL] EXPECT [SPEED] TO [SPEED]</Message>
    <Message Response="NE">WHEN CAN YOU ACCEPT [SPEED]</Message>
  </Category>
  <Category name="Comms">
    <Message Response="WU">CONTACT [UNIT] [FREQUENCY]</Message>
    <Message Response="WU">AT [POSITION] CONTACT [UNIT] [FREQUENCY]</Message>
    <Message Response="WU">AT [TIME] CONTACT [UNIT] [FREQUENCY]</Message>
    <Message Response="WU">MONITOR [UNIT] [FREQUENCY]</Message>
    <Message Response="WU">AT [POSITION] MONITOR [UNIT] [FREQUENCY]</Message>
    <Message Response="WU">AT [TIME] MONITOR [UNIT] [FREQUENCY]</Message>
    <Message Response="WU">SQUAWK [SSR]</Message>
    <Message Response="WU">STOP SQUAWK</Message>
    <Message Response="WU">SQUAWK ALTITUDE</Message>
    <Message Response="WU">STOP SQUAWK ALTITUDE</Message>
    <Message Response="WU">SQUAWK IDENT</Message>
    <Message Response="R">ALTIMETER [QNH]</Message>
    <Message Response="R">IDENTIFICATION</Message>
    <Message Response="R">IDENTIFIED</Message>
    <Message Response="R">IDENTIFICATION TERMINATED</Message>
    <Message Response="R">TRANSMIT ADS-B IDENT</Message>
    <Message Response="R">CONFIRM ADS-C EMERGENCY</Message>
    <Message Response="R">CHECK STUCK MICROPHONE [FREQUENCY]</Message>
    <Message Response="R">ATIS [CODE]</Message>
  </Category>
  <Category name="Report">
    <Message Response="R">REPORT BACK ON ROUTE</Message>
    <Message Response="R">REPORT LEAVING [LEVEL]</Message>
    <Message Response="R">REPORT LEVEL [LEVEL]</Message>
    <Message Response="R">REPORT REACHING [LEVEL]</Message>
    <Message Response="R">REPORT REACHING BLOCK [LEVEL] TO [LEVEL]</Message>
    <Message Response="R">REPORT PASSING [POSITION]</Message>
    <Message Response="NE">REPORT DISTANCE [TO/FROM] [POSITION]</Message>
    <Message Response="NE">REPORT REMAINING FUEL AND SOULS ON BOARD</Message>
    <Message Response="NE">CONFIRM POSITION</Message>
    <Message Response="NE">CONFIRM ALTITUDE</Message>
    <Message Response="NE">CONFIRM SPEED</Message>
    <Message Response="NE">CONFIRM ASSIGNED ALTITUDE</Message>
    <Message Response="NE">CONFIRM ASSIGNED SPEED</Message>
    <Message Response="NE">CONFIRM ASSIGNED ROUTE</Message>
    <Message Response="NE">CONFIRM TIME OVER REPORTED WAYPOINT</Message>
    <Message Response="NE">CONFIRM REPORTED WAYPOINT</Message>
    <Message Response="NE">CONFIRM NEXT WAYPOINT</Message>
    <Message Response="NE">CONFIRM NEXT WAYPOINT ETA</Message>
    <Message Response="NE">CONFIRM ENSUING WAYPOINT</Message>
    <Message Response="NE">CONFIRM REQUEST</Message>
    <Message Response="NE">CONFIRM SQUAWK</Message>
    <Message Response="NE">CONFIRM HEADING</Message>
    <Message Response="NE">CONFIRM GROUND TRACK</Message>
    <Message Response="NE">CONFIRM ATIS CODE</Message>
    <Message Response="NE">REQUEST POSITION REPORT</Message>
  </Category>
  <Category name="Misc">
    <Message Response="R">REQUEST RECEIVED RESPONSE WILL BE VIA VOICE</Message>
    <Message Response="NE">WHEN READY</Message>
    <Message Response="NE">THEN</Message>
    <Message Response="NE">DUE TO TRAFFIC</Message>
    <Message Response="NE">DUE TO AIRSPACE RESTRICTION</Message>
    <Message Response="R">DISREGARD</Message>
    <Message Response="WU">MAINTAIN OWN SEPARATION AND VMC</Message>
    <Message Response="NE">AT PILOTS DISCRETION</Message>
    <Message Response="NE">STANDBY</Message>
    <Message Response="NE">UNABLE</Message>
    <Message Response="NE">ROGER</Message>
    <Message Response="NE">AFFIRM</Message>
    <Message Response="NE">NEGATIVE</Message>
    <Message Response="R">ROGER MAYDAY</Message>
    <Message Response="R">ROGER PAN</Message>
  </Category>
</Messages>

*/