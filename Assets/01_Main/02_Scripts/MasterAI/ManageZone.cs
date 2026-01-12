using System.Collections.Generic;
using UnityEngine;

public class ManageZone
{
    private List<ZoneInfo> _allZones;
    private ZoneInfo _playerCurrentZone;

    public ManageZone(List<ZoneInfo> allZones)
    {
        _allZones = allZones;
        _playerCurrentZone = null;
    }
}
