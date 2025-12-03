#QuickStat

## Installasjon

QuickStat krever ingen installasjon.  Den legges vanligvis i mappen `.\bin` relativt til `FastTrak.exe`, altså ett hakk under "roten" på installasjonen. Dette er bare en konvensjon, mappen kan hete hva som helst og programmet kan ligge hvor som helst.

## Konfigurasjon

QuickStat krever en konfigurasjonsfil som inneholder en liste over tilkoblingsstrenger og protokoller man skal kunne koble seg til.  Den skal ha navnet Quickstat.Config.xml og ligge på samme sted som QuickStat.exe.  Dette er et eksempel på en slik fil:

    <?xml version="1.0"?>
    <QuickStat>
      <Connections>
      	<Connection>
      	  <Name>Testdatabase (NDV)</Name>
      	  <StudyName>NDV</StudyName>
      	  <ConnectionString>FILE NAME=..\FastTrak.UDL</ConnectionString>
      	</Connection>
      </Connections>
    </QuickStat>

Det er mulig å legge til flere **<Connection\>** tagger på samme måte, og de kan gå mot ulike databaser og prosjekter.

## Rettigheter

Brukere som skal ha tilgang til **QuickStat** må ha en egen databaserolle i aktuelle databaser, også kalt **QuickStat**.