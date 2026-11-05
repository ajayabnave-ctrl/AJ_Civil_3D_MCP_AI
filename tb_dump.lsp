(vl-load-com)
(defun c:TBDUMP (/ d f bd o p q)
  (setq d (vla-get-activedocument (vlax-get-acad-object)))
  (setq f (open "C:/RevitAddinDev/CIVIL 3D_MCP Setup/tb_dump2.txt" "w"))
  (vlax-for b (vla-get-block (vla-get-activelayout d))
    (write-line
      (strcat (vla-get-handle b) "|PS|" (vla-get-objectname b) "|" (vla-get-layer b)
        (if (vlax-property-available-p b 'TextString) (strcat "|" (vla-get-textstring b)) "")
        (if (= (vla-get-objectname b) "AcDbBlockReference")
          (strcat "|" (vla-get-name b) "|XREF="
            (if (= :vlax-true (vla-get-isxref (vla-item (vla-get-blocks d) (vla-get-name b)))) "Y" "N"))
          ""))
      f)
    (if (= (vla-get-objectname b) "AcDbBlockReference")
      (progn
        (setq bd (vla-item (vla-get-blocks d) (vla-get-name b)))
        (write-line (strcat "== DEF " (vla-get-name bd)) f)
        (vlax-for e bd
          (setq o (vla-get-objectname e))
          (cond
            ((wcmatch o "AcDbText,AcDbMText,AcDbAttributeDefinition")
             (setq p (vlax-get e 'InsertionPoint))
             (write-line (strcat (vla-get-handle e) "|" o "|" (vla-get-layer e) "|"
                                 (rtos (car p) 2 2) "," (rtos (cadr p) 2 2) "|" (vla-get-textstring e)) f))
            ((= "AcDbBlockReference" o)
             (setq p (vlax-get e 'InsertionPoint))
             (write-line (strcat (vla-get-handle e) "|BLK|" (vla-get-name e) "|"
                                 (rtos (car p) 2 2) "," (rtos (cadr p) 2 2)) f))
            ((= "AcDbLine" o)
             (setq p (vlax-curve-getstartpoint e) q (vlax-curve-getendpoint e))
             (write-line (strcat (vla-get-handle e) "|LINE|" (vla-get-layer e) "|"
                                 (rtos (car p) 2 2) "," (rtos (cadr p) 2 2) "->"
                                 (rtos (car q) 2 2) "," (rtos (cadr q) 2 2)) f))
            (T (write-line (strcat (vla-get-handle e) "|" o "|" (vla-get-layer e)) f)))))))
  (close f)
  (princ "\nTBDUMP_OK")
  (princ))

