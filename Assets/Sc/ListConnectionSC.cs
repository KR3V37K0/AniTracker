using UnityEngine;
using System.Data;
using Mono.Data;
using Mono.Data.Sqlite;
using System.IO;
using System.EnterpriseServices;
using static Unity.Burst.Intrinsics.X86;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Globalization;
using System;
using System.Threading;
using UnityEngine.UIElements;
using Unity.VisualScripting;

public class ListConnectionSC : MonoBehaviour
{
    string conn = SetDataBaseClass.SetDataBase("List.db");
    IDbConnection dbconn;
    IDbCommand dbcmd;
    IDataReader reader;
}
