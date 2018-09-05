<%@ Page Title="Home" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="TrunkedPrototypes._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="jumbotron">
        <h1>Image Recognition, Machine Learning & Barcode Scanning Prototypes</h1>
        <div >
            <ul>
                <li><a runat="server" href="~/VisionAPI.aspx">GoogleVisionAPI</a></li>
                <li><a runat="server" href="~/AutoML.aspx">GoogleCustom</a></li>
                <li><a runat="server" href="~/GoogleOCR.aspx">GoogleOCR</a></li>
                <li><a runat="server" href="~/GoogleBooksOCR.aspx">GoogleBooksOCR</a></li>
                <li><a runat="server" href="~/GoogleBooksOCRAzureVision.aspx">GoogleBooksOCRAzureVision</a></li>
                <li><a runat="server" href="~/Barcode.aspx">Barcodes</a></li>
                <li><a runat="server" href="~/AzureVisionAPI.aspx">AzureVisionAPI</a></li>
                <li><a runat="server" href="~/CustomVision.aspx">AzureCustom</a></li>
                <li><a runat="server" href="~/AzureOCR.aspx">AzureOCR</a></li>
            </ul>
        </div>
    </div>
</asp:Content>
