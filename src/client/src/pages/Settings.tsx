import { useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Trash2, Save, AlertCircle } from "lucide-react"
import { apiGet, apiPost } from "@/lib/api"

interface LogRetentionDateResponse {
  date: string | null
}

interface DeleteLogsResponse {
  executionsDeleted: number
  logsDeleted: number
  message: string
}

export function Settings() {
  const navigate = useNavigate()
  const [logRetentionDate, setLogRetentionDate] = useState<string>("")
  const [isLoading, setIsLoading] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [deleteResult, setDeleteResult] = useState<DeleteLogsResponse | null>(null)

  useEffect(() => {
    const fetchSettings = async () => {
      try {
        const token = sessionStorage.getItem("token")
        if (!token) {
          navigate("/login")
          return
        }

        const response: LogRetentionDateResponse = await apiGet<LogRetentionDateResponse>(
          "/api/settings/log-retention-date"
        )

        if (response.date) {
          // Convert date string to YYYY-MM-DD format for input
          const date = new Date(response.date)
          setLogRetentionDate(date.toISOString().split('T')[0])
        }
      } catch (err) {
        console.error("Error fetching log retention date:", err)
      }
    }

    fetchSettings()
  }, [navigate])

  const handleSave = async () => {
    setIsLoading(true)
    setError(null)
    setSuccess(null)

    try {
      await apiPost("/api/settings/log-retention-date", {
        date: logRetentionDate || null
      })

      setSuccess("Log retention date saved successfully")
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save log retention date")
    } finally {
      setIsLoading(false)
    }
  }

  const handleDeleteLogs = async () => {
    if (!logRetentionDate) {
      setError("Please set a log retention date first")
      return
    }

    if (!confirm(`Are you sure you want to delete all logs before ${logRetentionDate}? This action cannot be undone.`)) {
      return
    }

    setIsDeleting(true)
    setError(null)
    setSuccess(null)
    setDeleteResult(null)

    try {
      const response: DeleteLogsResponse = await apiPost<DeleteLogsResponse>(
        "/api/settings/delete-logs-before-date",
        {
          beforeDate: logRetentionDate
        }
      )

      setDeleteResult(response)
      setSuccess(response.message)
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete logs")
    } finally {
      setIsDeleting(false)
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Settings</h1>
        <p className="text-muted-foreground mt-2">
          Configure your application preferences
        </p>
      </div>

      {/* Log Retention Settings */}
      <div className="rounded-lg border bg-card p-6 shadow-sm">
        <div className="space-y-6">
          <div>
            <h3 className="text-lg font-semibold mb-4">Log Retention</h3>
            <p className="text-sm text-muted-foreground mb-4">
              Configure when to delete old backup logs. Logs older than the specified date will be deleted when you click the delete button.
            </p>
            
            <div className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="log-retention-date">Delete logs before date</Label>
                <Input
                  id="log-retention-date"
                  type="date"
                  value={logRetentionDate}
                  onChange={(e) => setLogRetentionDate(e.target.value)}
                  className="max-w-xs"
                />
                <p className="text-xs text-muted-foreground">
                  Select a date. All logs and executions before this date will be deleted.
                </p>
              </div>

              <div className="flex items-center gap-4">
                <Button
                  onClick={handleSave}
                  disabled={isLoading}
                  variant="default"
                >
                  <Save className="h-4 w-4 mr-2" />
                  Save Date
                </Button>

                <Button
                  onClick={handleDeleteLogs}
                  disabled={isDeleting || !logRetentionDate}
                  variant="destructive"
                >
                  <Trash2 className="h-4 w-4 mr-2" />
                  {isDeleting ? "Deleting..." : "Delete Logs Before Date"}
                </Button>
              </div>

              {error && (
                <div className="rounded-md bg-destructive/15 p-3 text-sm text-destructive flex items-center gap-2">
                  <AlertCircle className="h-4 w-4" />
                  {error}
                </div>
              )}

              {success && (
                <div className="rounded-md bg-green-500/15 p-3 text-sm text-green-600 dark:text-green-400">
                  {success}
                </div>
              )}

              {deleteResult && (
                <div className="rounded-lg border bg-muted p-4 space-y-2">
                  <p className="text-sm font-medium">Deletion Summary:</p>
                  <ul className="text-sm text-muted-foreground space-y-1 list-disc list-inside">
                    <li>Executions deleted: {deleteResult.executionsDeleted}</li>
                    <li>Log entries deleted: {deleteResult.logsDeleted}</li>
                  </ul>
                  <p className="text-xs text-muted-foreground mt-2">
                    Database has been optimized to free disk space.
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Legacy Preferences Section (keeping for now) */}
      <div className="rounded-lg border bg-card p-6 shadow-sm">
        <div className="space-y-6">
          <div>
            <h3 className="text-lg font-semibold mb-4">Preferences</h3>
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium">Dark Mode</label>
                  <p className="text-sm text-muted-foreground">
                    Toggle dark mode theme
                  </p>
                </div>
                <input type="checkbox" className="h-4 w-4" />
              </div>
              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium">Notifications</label>
                  <p className="text-sm text-muted-foreground">
                    Enable email notifications
                  </p>
                </div>
                <input type="checkbox" className="h-4 w-4" defaultChecked />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
