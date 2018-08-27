<%@ Page Title="QuaggaJS Barcode" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="QuaggaJSBarcode.aspx.cs" Inherits="TrunkedPrototypes.QuaggaJSBarcode" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">	
	<script>
		function showLoadingGif()
		{
			$("#imgLoading").show();
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
                        .then(function(result) {
                            document.querySelector('input.isbn').value = result.codeResult.code;
                        })
                        .catch(function() {
                            document.querySelector('input.isbn').value = "Not Found";
                        })
                        .then(function() {
                            this.attachListeners();
                        }.bind(this));
                },
                attachListeners: function() {
                    var self = this,
                        button = document.querySelector('.input-field input + .button.scan'),
                        fileInput = document.querySelector('.input-field input[type=file]');

                    button.addEventListener("click", function onClick(e) {
                        e.preventDefault();
                        button.removeEventListener("click", onClick);
                        document.querySelector('.input-field input[type=file]').click();
                    });

                    fileInput.addEventListener("change", function onChange(e) {
                        e.preventDefault();
                        fileInput.removeEventListener("change", onChange);
                        if (e.target.files && e.target.files.length) {
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
        <p class="lead">This uses QuaggaJS barcode decoder.<br />Choose an image with a barcode and then click the <strong>"Recognize Image"</strong> button.</p>
		<form>
            <div class="input-field">
                <input type="file" id="file" capture/>
                <button type="button" class="icon-barcode button scan">Decode</button>
                <label for="isbn_input">EAN:</label>
                <input id="isbn_input" class="isbn" type="text" />
            </div>
        </form>
        <asp:Label ID="lblDecodedBarcode" runat="server" Visible="false" CssClass="hiddenLabel"/>
        <label id="lblResultBarcode" />
    </div>
</asp:Content>
