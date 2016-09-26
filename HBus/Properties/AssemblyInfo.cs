using System.Reflection;
using System.Runtime.InteropServices;

// Le informazioni generali relative a un assembly sono controllate dal seguente 
// set di attributi. Per modificare le informazioni associate a un assembly
// occorre quindi modificare i valori di questi attributi.

[assembly: AssemblyTitle("HBus")]
[assembly: AssemblyDescription("HBus communication library")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("VM")]
[assembly: AssemblyProduct("HBus controller library")]
[assembly: AssemblyCopyright("Copyright ©  2014-2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Se si imposta ComVisible su false, i tipi in questo assembly non saranno visibili 
// ai componenti COM. Se è necessario accedere a un tipo in questo assembly da 
// COM, impostare su true l'attributo ComVisible per tale tipo.
[assembly: ComVisible(false)]

// Se il progetto viene esposto a COM, il GUID che segue verrà utilizzato per creare l'ID della libreria dei tipi
[assembly: Guid("d5cde980-8202-4aa6-8340-55e2f404203d")]

// Le informazioni sulla versione di un assembly sono costituite dai seguenti quattro valori:
//
//      Numero di versione principale
//      Numero di versione secondario 
//      Numero build
//      Revisione
//
// È possibile specificare tutti i valori oppure impostare valori predefiniti per i numeri relativi alla revisione e alla build 
// utilizzando l'asterisco (*) come descritto di seguito:
// [assembly: AssemblyVersion("1.0.*")]
//-----------------------------------------------------------------------------------------------------------------------------
// HBus Library (protocol version 2.0)
// 2.0.* - 07/01/2014: Library ricompilation with old references to version 1.0 eliminated
// 2.1.* - 24/01/2014: COde reorganization
// 2.1.* - 28/01/2014: Implemented abstract class Port and namespace revision
// 2.1.* - 19/07/2015: Utility class Csv
// 2.2.* - 30/07/2015: Architecture optimization
// 2.3.* - 30/09/2015: Code revision for Github publication
// 2.4.* - 12/09/2016: Created PortZMq with ZeroMq message queue
//-----------------------------------------------------------------------------------------------------------------------------

[assembly: AssemblyVersion("2.4.*")]
[assembly: AssemblyFileVersion("2.4.0")]