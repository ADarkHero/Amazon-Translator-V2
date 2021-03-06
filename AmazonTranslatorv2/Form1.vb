﻿Imports System.IO
Imports System.Text
Imports Microsoft.VisualBasic.FileIO
Imports System.Threading
Imports System.Text.RegularExpressions

Public Class Form1

    Dim srcDirectory As String = "C:\eNVenta-ERP\Auftragsimport\Amazon\reports\production\reports\"
    Dim doneReports As String = "C:\eNVenta-ERP\Auftragsimport\Amazon\reports\production\reports\done\"
    Dim destDirectory As String = "C:\eNVenta-ERP\Auftragsimport\Amazon\"
    Dim logDirectory As String = "C:\eNVenta-ERP\Auftragsimport\Amazon\debug\"
    Dim lastFilePath As String = "C:\eNVenta-ERP\Auftragsimport\Amazon\debug\lastFile.txt"
    Dim orderName As String = "ORDER*.txt"
	Dim filename As String = "AmazonOrders.csv"
	Dim logFile As String = "log.log"

	Dim text_file_path As String = ""
	Dim text_file_path_new As String = ""
	Dim file_dest_logAll As String = ""
	Dim file_old As String = ""
	Dim file_data As String
	Dim file_text As String = ""
	Dim lastFile As String = ""

	Dim file_dest As String = destDirectory + filename

	Dim fileExists As Boolean = False

    Dim shippingCosts As New Dictionary(Of String, Double)
    Dim amazonFeeAmount As Double = 0.135

    Dim thread As New Thread(AddressOf mainFunction)



	' Starts the main function, after the gui is shown
	Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Shown

		' The tread does all the important stuff.
		Try
			thread.Start()
		Catch ex As Exception

		End Try


	End Sub


	' Main loop of the program
	Private Sub mainFunction()

		While (True)
			While (True)
				writeLog("S", "PROCESS STARTED", "", 0)


				' What's the latest report that got written to the system?
				lastFile = My.Computer.FileSystem.ReadAllText(lastFilePath)

				' Read latest amazon report
				Dim dirs As String() = Directory.GetFiles(srcDirectory, orderName)





				If (fileIsEmpty()) Then
					Exit While
				End If

				If (fileAlreadyExists()) Then
					Exit While
				End If

				If (moveOldFiles()) Then
					Exit While
				End If


				If (checkNewReports(dirs)) Then
					Exit While
				End If



				doAmazonTranslation()

				writeLastFile()




				writeLog("R", "Sucessfully read report.", text_file_path, 300)

				FileClose()

			End While
		End While
	End Sub

	' If there is a file with 0kb, delete it, it will only make problems else
	Private Function fileIsEmpty() As Boolean
		If File.Exists(file_dest) Then
			If FileLen(file_dest) <= 0 Then
				Try
					File.Delete(file_dest)
					writeLog("F", "0 kb file got deleted", "", 0)
				Catch ex As Exception
					writeLog("F", ex.ToString, "", 60)
				End Try
				Return True
			End If
		End If

		Return False
	End Function

	' If the file already exists or if there are no new reports: restart the loop
	Private Function fileAlreadyExists() As Boolean

		If File.Exists(file_dest) Then
			writeLog("W", "File already exists.", file_dest, 300)
			Return True
		End If

		Return False
	End Function



	' Moves files that got read into the system
	' If files can't be moved, restart the loop
	Private Function moveOldFiles() As Boolean
		Dim wordArr As String() = lastFile.Split("\")
		Dim result As String = wordArr(1)
		Dim doneReportsFile As String = doneReports + wordArr(wordArr.Length - 1)
		If File.Exists(lastFile) Then
			Try
				File.Move(lastFile, doneReportsFile)
				writeLog("W", "File moved.", lastFile + " TO " + doneReportsFile, 0)
			Catch ex As Exception
				Try
					writeLog("F", ex.ToString, lastFile + " + " + doneReportsFile, 60)
					File.Delete(doneReportsFile)
				Catch ex2 As Exception
					writeLog("F", ex2.ToString, lastFile + " + " + doneReportsFile, 60)
				End Try
				Return True
			End Try
		End If
		Return False
	End Function


	' Read latest amazon report
	' If there are no files, restart the loop
	Private Function checkNewReports(dirs As String()) As Boolean

		Try
			dirs = Directory.GetFiles(srcDirectory, orderName)
			text_file_path = dirs(dirs.Length - 1)
		Catch ex As Exception
			writeLog("W", "No reports left.", "", 600)
			Return True
		End Try

		Return False
	End Function

	' Writes the Amazon.csv
	Private Function doAmazonTranslation()
        Dim data As System.IO.StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(file_dest, False)

        Dim old_order As String = ""
		Dim new_price As Double = 0 ' Price of one article
		Dim num_of_articles As Double = 1   ' Number of articles ordered
        Dim amazonFeePrice As Double





        ' Read shipping costs from file

        Dim tfpShipping As New TextFieldParser(text_file_path, Encoding.GetEncoding(65001))  ' Source files are in ANSI-encoding
        tfpShipping.Delimiters = New String() {vbTab}
        tfpShipping.TextFieldType = FieldType.Delimited

        tfpShipping.ReadLine() 'Skips header
        While tfpShipping.EndOfData = False
            file_data = tfpShipping.ReadLine()
            file_data = Replace(file_data, ";", ",")
            file_data = Replace(file_data, vbTab, ";")
            Dim file_data_array As String() = file_data.Split(New Char() {";"c})

            'If the order number already exists, add shipping costs to order
            If shippingCosts.ContainsKey(file_data_array(0)) Then
                shippingCosts(file_data_array(0)) = shippingCosts(file_data_array(0)) + file_data_array(13) / 100

                'If the order number does not exist, add it
            Else
                shippingCosts.Add(file_data_array(0), file_data_array(13) / 100)
            End If
        End While

        data.Dispose()
        data.Close()
        tfpShipping.Dispose()
        tfpShipping.Close()

        'Read actual data from file

        data = My.Computer.FileSystem.OpenTextFileWriter(file_dest, False)

        Dim tfp As New TextFieldParser(text_file_path, Encoding.GetEncoding(65001))  ' Source files are in ANSI-encoding
        tfp.Delimiters = New String() {vbTab}
        tfp.TextFieldType = FieldType.Delimited
        tfp.ReadLine() 'Skips header
		While tfp.EndOfData = False
			file_data = tfp.ReadLine()
			file_data = Replace(file_data, ";", ",")
			file_data = Replace(file_data, vbTab, ";")
			Dim file_data_array As String() = file_data.Split(New Char() {";"c})

			file_data = ""
			For value As Integer = 0 To file_data_array.Length
				' Changes Parameters to shipping parameters
				Select Case value
					Case 11
						' Fix for prices
						' Current price divided by number of articles
						num_of_articles = Convert.ToDouble(file_data_array(9))
						new_price = Convert.ToDouble(file_data_array(value)) / 100 / num_of_articles
						Dim new_price_string As String = Replace(new_price.ToString, ",", ".")   ' We need a . for decimals
						file_data = file_data + new_price_string + ";"
					Case 13
                        file_data = file_data + Replace(shippingCosts(file_data_array(0)).ToString, ",", ".") + ";"
                    Case 17
						' Do Stuff for 17 in step 18
					Case 18
						' User is private, not business
						If file_data_array(value).Length = 0 Then
							file_data = file_data + file_data_array(value - 1) + ";;"
						Else
							file_data = file_data + file_data_array(value) + ";" + file_data_array(value - 1) + ";"
						End If
                    Case 26
                        ' Do Stuff for 25 in step 26
                    Case 27
                        ' User is private, not business
                        ' Business people have their business name in the field, where private people have their street
                        If file_data_array(value).Length = 0 Then
                            file_data = file_data + file_data_array(value - 1) + ";;"
                        Else
                            file_data = file_data + file_data_array(value) + ";" + file_data_array(value - 1) + ";"
                        End If
                    Case file_data_array.Length
                        amazonFeePrice = ((new_price * num_of_articles + shippingCosts(file_data_array(0)) / 100) * amazonFeeAmount / num_of_articles) / 116 * 100
                        file_data = file_data + Replace(amazonFeePrice.ToString, ",", ".")
					Case Else
						file_data = file_data + file_data_array(value) + ";"
				End Select
			Next

			data.WriteLine(file_data)


            old_order = file_data_array(0)
		End While

		data.Dispose()
		data.Close()
		tfp.Dispose()
		tfp.Close()
	End Function


	' Writes the file path of the last used report to a txt file
	' The same file should not be read in twice and moved, if it isn't needed anymore
	Private Function writeLastFile()
		' Write the name of the last read file to txt
		Dim lastFileWriter As System.IO.StreamWriter
		lastFileWriter = My.Computer.FileSystem.OpenTextFileWriter(lastFilePath, False)
		lastFileWriter.Write(text_file_path.ToString)
		lastFileWriter.Dispose()
		lastFileWriter.Close()
	End Function




	' Writes logs to a text file
	Private Function writeLog(errortype As String, message As String, file As String, waittime As Int16)
		Dim logMessage As String = ""
		If (errortype <> "") Then
			logMessage = logMessage + "[" + errortype + "]"
		End If
		If (message <> "") Then
			logMessage = logMessage + " " + message
		End If
		If (file <> "") Then
			logMessage = logMessage + " > " + file
		End If
		If (waittime <> 0) Then
			Dim waitminute As Double = waittime / 60
			logMessage = logMessage + " | Waiting " + waitminute.ToString + " minute"
			Try
				If (waitminute > 1) Then
					logMessage = logMessage + "s"
				End If
			Catch ex As Exception
				logMessage = logMessage + "s"
			End Try
			logMessage = logMessage + "."
		End If


		Try
			IO.File.AppendAllText(logDirectory + logFile, String.Format("{0}{1}", Environment.NewLine, DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + " " + logMessage))
		Catch ex As Exception

		End Try

		Threading.Thread.Sleep(waittime * 1000)

	End Function


	Private Sub openReports_Click(sender As Object, e As EventArgs) Handles openReports.Click
		Process.Start(srcDirectory)
	End Sub

	Private Sub openAmazon_Click(sender As Object, e As EventArgs) Handles openAmazon.Click
		Process.Start(destDirectory)
	End Sub

	Private Sub openLog_Click(sender As Object, e As EventArgs) Handles openLog.Click
		Process.Start(logDirectory + logFile)
	End Sub

	Private Sub openDoneReports_Click(sender As Object, e As EventArgs) Handles openDoneReports.Click
		Process.Start(doneReports)
	End Sub

	Private Sub openLogFolder_Click(sender As Object, e As EventArgs) Handles openLogFolder.Click
		Process.Start(logDirectory)
	End Sub

	Private Sub openDebug_Click(sender As Object, e As EventArgs) Handles openDebug.Click
		Process.Start("Z:\Automatisierung\Projects\AmazonTranslatorv2\AmazonTranslatorv2\bin\Debug")
	End Sub

	Private Sub openAMTU_Click(sender As Object, e As EventArgs) Handles openAMTU.Click
		Process.Start("C:\Program Files (x86)\AMTU\Amazon Merchant Transport Utility.exe")
	End Sub

	Private Sub killProgram_Click(sender As Object, e As EventArgs) Handles killProgram.Click
		thread.Abort()
		Application.Exit()
		End
	End Sub
End Class
