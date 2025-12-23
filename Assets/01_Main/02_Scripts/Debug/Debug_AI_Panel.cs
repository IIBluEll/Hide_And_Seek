using TMPro;
using UnityEngine;

public class Debug_AI_Panel : MonoBehaviour
{
    public MasterAI_Provider AI_Provider;

    public TMP_Text TensionValueTxt;
    public TMP_Text StartZoneTxt;
    public TMP_Text EngineRoomTxt;
    public TMP_Text RestaruantTxt;
    public TMP_Text WareHouseTxt;

    private void Update()
    {
        StartZoneTxt.text = $"START_ZONE Value : {AI_Provider.GetZoneSuspicion(EZONE_ID.START_ZONE).ToString()}";

        EngineRoomTxt.text = $"ENGINEROOM_ZONE Value : {AI_Provider.GetZoneSuspicion(EZONE_ID.ENGINEROOM_ZONE).ToString()}";

        RestaruantTxt.text = $"RESTAURANT_ZONE Value : {AI_Provider.GetZoneSuspicion(EZONE_ID.RESTAURANT_ZONE).ToString()}"; 

        WareHouseTxt.text = $"WAREHOUSE_ZONE Value : {AI_Provider.GetZoneSuspicion(EZONE_ID.WAREHOUSE_ZONE).ToString()}";
    }
}
