<%@ Page Title="Barcode" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="QuaggaJSBarcode.aspx.cs" Inherits="Trunked.QuaggaJSBarcode" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">	
	<script>
		function showLoadingGif() {
			$("#imgLoading").show();
        }

        function hideLoadingGif() {
			$("#imgLoading").hide();
		}
	</script>
    <script src="/Scripts/quagga.min.js" type="text/javascript"></script>
    <script src="/Scripts/quagga.js" type="text/javascript"></script>
    <script>
        $(document).ready(function () {
            var Quagga = window.Quagga;
            var App = {
                _scanner: null,
                init: function() {
                    this.attachListeners();
                },
                decode: function(file) {
                    Quagga
                        .decoder({readers: ['ean_reader']})
                        .locator({patchSize: 'medium'})
                        .fromSource(file, {size: 800})
                        .toPromise()
                        .then(function (result) {
                            // This is the function that gets called after the barcode has been decoded
                            hideLoadingGif();
                            $('#<%=lblResult.ClientID%>').html("Barcode found: " + result.codeResult.code);
                            $('#<%=hdnResult.ClientID%>').val(result.codeResult.code);
                            document.getElementById('<%=btnGetBookInfo.ClientID%>').style.visibility = 'visible';
                            return false;  
                        })
                        .catch(function () {
                            // Handle when no barcode is found
                            hideLoadingGif();
                            $('#<%=lblResult.ClientID%>').html("No barcode found");
                            $('#<%=hdnResult.ClientID%>').val("No barcode found");
                            return false;
                        })
                        .then(function() {
                            this.attachListeners();
                        }.bind(this));
                },
                attachListeners: function() {
                    var self = this,
                        button = document.querySelector('.icon-barcode.button.scan.btn.btn-primary.btn-lg'),
                        fileInput = document.querySelector('.input-field input[type=file]');

                    // Adds a listener to trigger the deocde function when an image has been selected
                    fileInput.addEventListener("change", function onChange(e) {
                        e.preventDefault();
                        fileInput.removeEventListener("change", onChange);
                        if (e.target.files && e.target.files.length) {
                            showLoadingGif();
                            self.decode(e.target.files[0]);
                        }
                    });
                }
            };
            App.init();
        });
    </script>

	<div class="jumbotron">
        <h1>Barcodes</h1>
        <p class="lead">Select an image of a barcode to decode:</p>

		<form>
            <div class="input-field">
                <input type="file" id="file" capture/><br />
                <img id="imgLoading" src="Content/Images/Loading.gif" style="width:30px;display:none;"/>
            </div>
        </form>

        <br />

        <p><asp:Label ID="lblResult" runat="server" Visible="true"/></p>
        <asp:HiddenField ID="hdnResult" runat="server" />

        <%-- Should try to get this working without the need of this button. So when we get the result, it automatically calls the method. --%>
        <asp:Button ID="btnGetBookInfo" runat="server" CssClass="btn btn-primary btn-lg" OnClick="btnGetBookInfo_Click" OnClientClick="showLoadingGif()" Text="Get Book Info" style="visibility:hidden" />

		<br />

		<asp:Table ID="tblResults" runat="server" GridLines="Both" Visible="false" />
    </div>
</asp:Content>
