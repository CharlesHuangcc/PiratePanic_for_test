pipeline {
    agent { label 'local-hccpc' } // 本地pc节点

    environment {
        WORKSPACE = env.WORKSPACE // 本地pc节点的工作目录
		PROJECT_ROOT = powershell( script: "Split-Path -Parent '${WORKSPACE}'", returnStdout: true).trim()		
		NAKAMASERVER_PATH = "${PROJECT_ROOT}\\NakamaServer"
		UNITY_CLIENT_PROJECT_PATH = "${PROJECT_ROOT}\\PiratePanic"
		PYTHON_TEST_PATH = "${PROJECT_ROOT}\\python_test"
		
		ALLURE_RESULTS = "${WORKSPACE}\\allure-results"
        ALLURE_REPORT = "${WORKSPACE}\\allure-report"
        BUILD_OUTPUT = "${WORKSPACE}\\BuildClient"
		
		UNITY_EXE = "C:\\Program Files\\Unity\\Hub\\Editor\\2022.3.44f1c1\\Editor\\Unity.exe"
		
		CONFIG_TEST_THRESHOLD = 90
        UNITY_EDIT_TEST_THRESHOLD = 90
        UNITY_PLAY_TEST_THRESHOLD = 90
		SERVER_UNIT_THRESHOLD = 90
        PROTOCOL_TEST_THRESHOLD = 90
        SMOKE_TEST_THRESHOLD = 100
        REGRESS_TEST_THRESHOLD = 90
		
    }

    stages {
        stage('1 代码拉取'){
			steps{
				echo '代码已拉取' // jenkins设置git hooks+轮询SCM			
			}
		}
		
		stage('2.1 配置检查'){
			steps{
				echo "执行Excel/CSV配置校验，待补充..."
				
				echo "执行UTF EditMode ScriptableObject配置校验"
				powershell """
					& ${UNITY_EXE} -batchmode -projectPath ${UNITY_CLIENT_PROJECT_PATH} `
					-runEditorTests `
					-testThreshold ${CONFIG_TEST_THRESHOLD} `
					--editorTestsResultFile ${ALLURE_RESULTS}\\unity_edit_config.xml
				"""
			}
			post{
				failure{
					error "2.1 配置检查失败，配置校验通过率低于阈值，流水线中断"
				}
			}
		}
		
		stage('2.2 客户端C#单元测试'){
			steps{
				echo "执行UTF Unity EditMode C#单元测试"
				powershell """
					& ${UNITY_EXE} -batchmode -projectPath ${UNITY_CLIENT_PROJECT_PATH} `
					-runEditorTests `
					-testThreshold ${CONFIG_TEST_THRESHOLD} `
					--editorTestsResultFile ${ALLURE_RESULTS}\\unity_edit_code.xml
				"""
			}
			post{
				failure{
					error "2.2 客户端C#单元测试失败，通过率低于阈值，流水线中断"
				}
			}
		}
		
		stage('2.3 Nakama服务端单元测试'){
			steps{
				echo "服务端业务逻辑单元测试，待补充..."
				
			}
			post{
				failure{
					error "2.3 服务端单元测试失败，低于阈值，流水线中断"
				}
			}
		}
		
		stage('3.1 Unity客户端编译构建'){
			steps{
				powershell """
					cd "${PYTHON_TEST_PATH}"
					uv sync
					uv run python unity_test/build_unity.py
				"""
			}
			post{
				failure{
                    error "3.1 客户端编译失败，Unity打包异常，流水线中断"
                }
			}
		}
		
		stage('3.2 启动服务器'){
			steps{
				echo "Docker重建nakama"
				powershell """
					cd "${NAKAMASERVER_PATH}"
					npx tsc
					Start-Sleep -Seconds 3
					docker compose up --build nakama
					Start-Sleep -Seconds 15
				"""
			}
			post {
				failure {
					powershell "docker-compose -f ${DOCKER_COMPOSE_FILE} logs"
					error "3.2 Nakama服务启动失败,容器构建/启动异常，流水线中断"
				}
			}
		}
		
		
		stage(`4.1 客户端集成测试`){
			steps{
				echo "执行UTF Unity PlayMode C#集成测试"
				powershell """
					& ${UNITY_EXE} -batchmode -projectPath ${CLIENT_PATH} `
                    -executeMethod UnityTestRunner.RunPlayModeIntegrateTest `
                    -testThreshold ${UNITY_PLAY_TEST_THRESHOLD} `
                    -testResults ${ALLURE_RESULTS}\\unity_play_integrate.xml `
                    -quit
				"""
			}
			post{
				failure {
                    error "4.1 客户端集成测试失败，PlayMode校验低于阈值，流水线中断"
                }
			}
		}
		
		stage('4.2 服务端协议&数据库联调Pytest'){
			steps{
				echo "Nakama协议收发，DB接口自动化测试"
				powershell """
					cd "${PYTHON_TEST_PATH}"
					uv sync
					uv run pytest nakama-test/
				"""
			}
			post{
				failure {
                    error "4.2 服务端协议测试失败，低于阈值，流水线中断"
                }
			}
		}
		
		stage('5.1 主流程冒烟测试'){
			steps{
				echo "启动客户端，执行登录/联机/战斗主流程冒烟测试，待补充..."
				
			}
			post{
				failure {
                        error "5.1 冒烟测试失败，游戏主流程存在BUG，流水线中断"
                }
			}
		}
		
		stage('5.2 全量回归测试'){
			steps{
				echo "执行完整回归测试用例集，待补充..."
				
			}
			post{
				failure {
                        error "5.2 冒烟测试失败，低于阈值，流水线中断"
                }
			}
		}
		
    }

    post {
        always {
            echo '测试流水线结束，生成完整测试报告，待补充...'
        }
		success {
			echo '成功-钉钉发送通知'
		}
        failure {
            echo '失败-钉钉发送通知'
        }
    }
}