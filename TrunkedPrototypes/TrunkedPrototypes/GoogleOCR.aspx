﻿<%@ Page Title="GoogleOCR" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="GoogleOCR.aspx.cs" Inherits="TrunkedPrototypes.GoogleOCR" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">	
	<script>
		function showLoadingGif()
		{
			$("#imgLoading").show();
		}
	</script>

	<div class="jumbotron">
        <h1>Google OCR</h1>
        <p class="lead">This uses Google OCR.<br />Choose an image and then click the <strong>"Recognize Image"</strong> button.</p>
		<p><asp:FileUpload ID="ctrlFileUpload" runat="server" /></p>

		<br />

		<asp:Label ID="lblStatus" runat="server" Visible="false" Text="Upload status: " />

		<p>
			<asp:Button ID="btnRecognize" CssClass="btn btn-primary btn-lg" runat="server" OnClientClick="showLoadingGif()" OnClick="RecognizeButton_Click" Text="Recognize Image" />
			<img id="imgLoading" src="Content/Images/Loading.gif" style="width:30px;display:none;"/>
		</p>	

		<br />

		<asp:Table ID="tblResults" runat="server" GridLines="Both" Visible="false" />
    </div>
</asp:Content>
