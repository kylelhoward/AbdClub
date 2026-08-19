By combining those specific CSS properties with an independent Razor 
file (Layout = null), you create a lightweight print blueprint.
Why This Style Block is a Printing Masterpiecepage-break-after:
 always;This is the command that makes multi-form printing possible. 
 Every time the browser encounters a div with the .print-page class,
  it stops rendering on the current sheet of paper and forces the
   printer hardware to advance to a brand-new page. 
   This guarantees your Active Members checklist never accidentally
    bleeds into the Event Duty Log.@@media print
     { .no-print { display: none !important; } }
This creates an interactive bridge between your web app 
and physical paper. When viewed on a laptop screen, 
the bright action bar button displays normally so officers can click it. 
The exact millisecond the browser sends the document to a printer 
or a PDF file stream, the CSS automatically intercepts it 
and hides that control bar, keeping your paper clean and 
free of messy web graphics.background-color:
#f2f2f2 !important; and color: #000 !important;
Browsers try to save ink by default, meaning they will 
automatically strip away background highlights on data rows and 
header grids. Forcing !important directly inside your 
table style parameters tells the modern browser rendering engine
 to override its ink-saving rules and print sharp, 
 gray table column headers. This gives your forms a clean, 
 professional clipboard appearance.The checkbox-square 
 Inline ElementInstead of struggling to align a 
 heavy browser checkbox widget, this trick uses a 
 simple HTML <span> styled into a raw vector square box 
 (14px tall by 14px wide with a solid border). This renders 
 instantly and looks pristine on paper, giving door hosts an
  ideal target to mark attendance with a physical pen.
  This simple block of CSS transforms standard web markup 
  into a highly reliable, cross-platform paper generation utility.