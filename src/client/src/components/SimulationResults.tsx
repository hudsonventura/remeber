import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { Button } from "@/components/ui/button"

interface SimulationItem {
  fileName: string
  filePath: string
  size: number | null
  action: string
  reason: string
}

interface SimulationResult {
  items: SimulationItem[]
  totalItems: number
  itemsToCopy: number
  itemsToDelete: number
}

interface SimulationResultsProps {
  open: boolean
  onClose: () => void
  result: SimulationResult | null
  isLoading: boolean
}

function formatFileSize(bytes: number | null): string {
  if (bytes === null || bytes === undefined) return "N/A"
  if (bytes === 0) return "0 B"
  
  const k = 1024
  const sizes = ["B", "KB", "MB", "GB", "TB"]
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${sizes[i]}`
}

export function SimulationResults({ open, onClose, result, isLoading }: SimulationResultsProps) {
  return (
    <AlertDialog open={open} onOpenChange={onClose}>
      <AlertDialogContent className="max-w-4xl max-h-[80vh] overflow-hidden flex flex-col">
        <AlertDialogHeader>
          <AlertDialogTitle>Backup Simulation Results</AlertDialogTitle>
          <AlertDialogDescription>
            Preview of what will happen when this backup plan runs
          </AlertDialogDescription>
        </AlertDialogHeader>
        
        {isLoading ? (
          <div className="flex items-center justify-center py-8">
            <p className="text-muted-foreground">Running simulation...</p>
          </div>
        ) : result ? (
          <div className="flex-1 overflow-hidden flex flex-col space-y-4">
            <div className="grid grid-cols-3 gap-4">
              <div className="rounded-lg border bg-card p-4">
                <p className="text-sm text-muted-foreground">Total Items</p>
                <p className="text-2xl font-bold">{result.totalItems}</p>
              </div>
              <div className="rounded-lg border bg-card p-4">
                <p className="text-sm text-muted-foreground">To Copy</p>
                <p className="text-2xl font-bold text-green-600 dark:text-green-400">{result.itemsToCopy}</p>
              </div>
              <div className="rounded-lg border bg-card p-4">
                <p className="text-sm text-muted-foreground">To Delete</p>
                <p className="text-2xl font-bold text-red-600 dark:text-red-400">{result.itemsToDelete}</p>
              </div>
            </div>

            {result.items.length === 0 ? (
              <div className="text-center py-8">
                <p className="text-muted-foreground">No changes detected. Source and destination are in sync.</p>
              </div>
            ) : (
              <div className="flex-1 overflow-auto border rounded-lg">
                <table className="w-full">
                  <thead className="bg-muted sticky top-0">
                    <tr>
                      <th className="text-left p-3 text-sm font-medium">File Name</th>
                      <th className="text-left p-3 text-sm font-medium">Size</th>
                      <th className="text-left p-3 text-sm font-medium">Action</th>
                      <th className="text-left p-3 text-sm font-medium">Reason</th>
                    </tr>
                  </thead>
                  <tbody>
                    {result.items.map((item, index) => (
                      <tr
                        key={index}
                        className="border-t hover:bg-muted/50"
                      >
                        <td className="p-3 text-sm">
                          <div className="max-w-md truncate" title={item.filePath}>
                            {item.fileName}
                          </div>
                        </td>
                        <td className="p-3 text-sm text-muted-foreground">
                          {formatFileSize(item.size)}
                        </td>
                        <td className="p-3 text-sm">
                          <span
                            className={`px-2 py-1 rounded text-xs font-medium ${
                              item.action === "Copy"
                                ? "bg-green-500/20 text-green-600 dark:text-green-400"
                                : "bg-red-500/20 text-red-600 dark:text-red-400"
                            }`}
                          >
                            {item.action}
                          </span>
                        </td>
                        <td className="p-3 text-sm text-muted-foreground">
                          {item.reason}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        ) : null}

        <div className="flex justify-end pt-4">
          <Button onClick={onClose}>Close</Button>
        </div>
      </AlertDialogContent>
    </AlertDialog>
  )
}

