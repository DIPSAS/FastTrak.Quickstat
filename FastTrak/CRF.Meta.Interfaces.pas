unit CRF.Meta.Interfaces;

interface

uses
  SysUtils;

type
  ICRFClickable = interface
    ['{2F30F0F2-DCE8-4CE3-A87F-F77963CF4464}']
  end;

  ECRFInvalid = class( Exception );
  ECRFInvalidMetadata = class( Exception );

function BoolToInt( const AValue: boolean ): integer;

const
  META_HOST = 'https://fasttrak.dips.no';
  URL_ITEM  = META_HOST + '/CRFShowItem.asp?ItemId=%d';
  URL_FORM  = META_HOST + '/CRFShowForm.asp?FormId=%d';

implementation

function BoolToInt( const AValue: boolean ): integer;
begin
  if AValue then
    Result := 1
  else
    Result := 0;
end;

end.
